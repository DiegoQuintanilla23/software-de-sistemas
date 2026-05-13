using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProyectoSoftwareSistemas
{
    public partial class CargadorLigador : Form
    {
        // Variables globales especificadas en el algoritmo
        private int DIRPROG = 0;
        private int DIRSC = 0;
        private int DIREJ = 0;

        private List<ArchivoObjeto> archivosCargados = new List<ArchivoObjeto>();
        private List<ElementoTABSE> TABSE = new List<ElementoTABSE>();

        // Memoria física simulada (diccionario para inicialización perezosa)
        private Dictionary<int, byte> memoriaFisica = new Dictionary<int, byte>();
        private HashSet<int> bytesModificados = new HashSet<int>();

        private int minDirVisual = 0xFFFFFF;
        private int maxDirVisual = 0;

        public CargadorLigador()
        {
            InitializeComponent();
            ConfigurarGrids();
        }

        private void CargadorLigador_Load(object sender, EventArgs e)
        {
            btnEjecutarPaso2.Enabled = false;
        }

        private void ConfigurarGrids()
        {
            if (dgvTabse.Columns.Count == 0)
            {
                dgvTabse.Columns.Add("Seccion", "Sección Control");
                dgvTabse.Columns.Add("Simbolo", "Símbolo");
                dgvTabse.Columns.Add("Direccion", "Dirección");
                dgvTabse.Columns.Add("Longitud", "Longitud");
                dgvTabse.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvTabse.AllowUserToAddRows = false;
                dgvTabse.RowHeadersVisible = false;
            }

            // Configuración Mapa de Memoria
            if (dgvMemoria.Columns.Count == 0)
            {
                dgvMemoria.Columns.Add("Dir", "Dir");
                dgvMemoria.Columns["Dir"].MinimumWidth = 60; // Cambiamos Width por MinimumWidth

                for (int i = 0; i < 16; i++)
                {
                    dgvMemoria.Columns.Add($"C{i}", i.ToString("X"));
                    dgvMemoria.Columns[$"C{i}"].MinimumWidth = 30; // Protegemos que no se hagan invisibles si se hace pequeña
                }

                // --- LA MAGIA RESPONSIVA ESTÁ AQUÍ ---
                dgvMemoria.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                dgvMemoria.AllowUserToAddRows = false;
                dgvMemoria.RowHeadersVisible = false;
                dgvMemoria.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void btnCargarPaso1_Click(object sender, EventArgs e)
        {
            if (archivosCargados.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(txtDirProg.Text) || !int.TryParse(txtDirProg.Text, System.Globalization.NumberStyles.HexNumber, null, out DIRPROG))
                {
                    MessageBox.Show("Por favor ingresa una Dirección de Carga del SO (DIRPROG) válida en hexadecimal.", "Falta DIRPROG", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Toma DIRPROG del Sistema Operativo
                // Asigna DIRSC = DIRPROG {para la primera sección de control}
                DIRSC = DIRPROG;
                txtDirProg.Enabled = false;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Archivos Objeto (*.obj)|*.obj|Todos los archivos (*.*)|*.*";
                ofd.Title = "Selecciona un Módulo Objeto (Paso 1)";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        List<string> lineasObj = File.ReadAllLines(ofd.FileName).ToList();
                        EjecutarPaso1(Path.GetFileName(ofd.FileName), lineasObj);
                        btnEjecutarPaso2.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error en Paso 1", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnEjecutarPaso2_Click(object sender, EventArgs e)
        {
            try
            {
                btnCargarPaso1.Enabled = false;
                btnEjecutarPaso2.Enabled = false;

                EjecutarPaso2();
                DibujarMapaMemoria();

                MessageBox.Show("Paso 2 completado. Relocalización y ligado finalizados.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al ligar los módulos:\n{ex.Message}", "Error en Paso 2", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            DIRPROG = 0; DIRSC = 0; DIREJ = 0;
            archivosCargados.Clear();
            TABSE.Clear();
            memoriaFisica.Clear();
            bytesModificados.Clear();
            minDirVisual = 0xFFFFFF; maxDirVisual = 0;

            txtDirProg.Enabled = true;
            txtDirProg.Clear();
            txtTamprog.Clear(); 
            btnCargarPaso1.Enabled = true;
            btnEjecutarPaso2.Enabled = false;

            lstArchivos.Items.Clear();
            dgvTabse.Rows.Clear();
            dgvMemoria.Rows.Clear();
        }

        // =========================================================================
        // ALGORITMO PASO 1. CARGADOR LIGADOR
        // =========================================================================
        private void EjecutarPaso1(string nombreArchivo, List<string> registros)
        {
            int LONSC = 0;
            string currentSeccion = "";

            // Usamos una lista temporal para que la carga sea ATÓMICA
            List<ElementoTABSE> tabseTemporal = new List<ElementoTABSE>();

            foreach (string regRaw in registros)
            {
                if (string.IsNullOrWhiteSpace(regRaw)) continue;
                string reg = regRaw.TrimEnd();

                if (reg.StartsWith("H"))
                {
                    if (reg.Length < 13) throw new Exception($"Registro H malformado: {reg}");

                    string longitudStr = reg.Substring(reg.Length - 6, 6);
                    LONSC = Convert.ToInt32(longitudStr, 16);
                    currentSeccion = reg.Substring(1, reg.Length - 13).Trim();

                    if (TABSE.Any(t => t.SeccionControl == currentSeccion) || tabseTemporal.Any(t => t.SeccionControl == currentSeccion))
                        throw new Exception($"Símbolo externo duplicado (Sección): {currentSeccion}");

                    tabseTemporal.Add(new ElementoTABSE
                    {
                        SeccionControl = currentSeccion,
                        Direccion = DIRSC,
                        Longitud = LONSC
                    });
                }
                else if (reg.StartsWith("D"))
                {
                    string datosD = reg.Substring(1);

                    // REGEX ROBUSTO: Encuentra pares de (Nombre) + (6 dígitos Hex)
                    var matches = System.Text.RegularExpressions.Regex.Matches(datosD, @"([A-Za-z_][A-Za-z0-9_]*)\s*([0-9A-Fa-f]{6})");

                    if (matches.Count == 0 && datosD.Length > 0)
                    {
                        while (datosD.Length >= 12)
                        {
                            string sim = datosD.Substring(0, 6).Trim();
                            int dirRelativa = Convert.ToInt32(datosD.Substring(6, 6), 16);

                            if (TABSE.Any(t => t.Simbolo == sim) || tabseTemporal.Any(t => t.Simbolo == sim))
                                throw new Exception($"Símbolo externo duplicado: {sim}");

                            tabseTemporal.Add(new ElementoTABSE { Simbolo = sim, Direccion = DIRSC + dirRelativa });
                            datosD = datosD.Substring(12);
                        }
                    }
                    else
                    {
                        foreach (System.Text.RegularExpressions.Match m in matches)
                        {
                            string sim = m.Groups[1].Value.ToUpper();
                            int dirRelativa = Convert.ToInt32(m.Groups[2].Value, 16);

                            if (TABSE.Any(t => t.Simbolo == sim) || tabseTemporal.Any(t => t.Simbolo == sim))
                                throw new Exception($"Símbolo externo duplicado: {sim}");

                            tabseTemporal.Add(new ElementoTABSE { Simbolo = sim, Direccion = DIRSC + dirRelativa });
                        }
                    }
                }
            }

            // Si no hubo errores, guardamos los cambios
            TABSE.AddRange(tabseTemporal);
            archivosCargados.Add(new ArchivoObjeto { NombreArchivo = nombreArchivo, Registros = registros });
            lstArchivos.Items.Add(nombreArchivo);
            ActualizarGridTabse();

            // Preparamos la dirección inicial para el siguiente módulo (¡Una sola vez!)
            DIRSC += LONSC;

            // Muestra el tamaño total acumulado del programa en hexadecimal
            txtTamprog.Text = (DIRSC - DIRPROG).ToString("X4") + "H";
        }

        private void ActualizarGridTabse()
        {
            dgvTabse.Rows.Clear();
            foreach (var e in TABSE)
            {
                dgvTabse.Rows.Add(
                    e.SeccionControl,
                    e.Simbolo,
                    e.Direccion.ToString("X6"),
                    e.Longitud > 0 ? e.Longitud.ToString("X6") : ""
                );
            }
        }

        // =========================================================================
        // ALGORITMO PASO 2 DEL CARGADOR LIGADOR
        // =========================================================================
        private void EjecutarPaso2()
        {
            // Asigna DIRSC = DIRPROG
            // Asigna DIREJ = DIRPROG
            DIRSC = DIRPROG;
            DIREJ = DIRPROG;

            // Mientras No se Termine la entrada Hacer
            foreach (var archivo in archivosCargados)
            {
                int LONSC = 0;

                foreach (string regRaw in archivo.Registros)
                {
                    if (string.IsNullOrWhiteSpace(regRaw)) continue;
                    string reg = regRaw.TrimEnd();

                    // Lee el siguiente registro de entrada {el registro de encabezamiento}
                    if (reg.StartsWith("H"))
                    {
                        // Asigna a LONSC la longitud de la sección de control
                        LONSC = Convert.ToInt32(reg.Substring(reg.Length - 6, 6), 16);
                    }
                    // Mientras el tipo de registro <> 'E' Hacer
                    // Si el tipo de registro = 'T' Entonces
                    else if (reg.StartsWith("T"))
                    {
                        int dirT = Convert.ToInt32(reg.Substring(1, 6), 16);
                        string codigoHex = reg.Substring(9);
                        int dirFisica = DIRSC + dirT;

                        if (dirFisica < minDirVisual) minDirVisual = dirFisica;
                        if (dirFisica + (codigoHex.Length / 2) > maxDirVisual) maxDirVisual = dirFisica + (codigoHex.Length / 2);

                        // {Si el código objeto esta en forma de caracteres, se convierte a la representación interna}
                        // Pasa el código objeto del registro a la localidad (DIRSC + dirección especificada)
                        for (int i = 0; i < codigoHex.Length; i += 2)
                        {
                            byte valor = Convert.ToByte(codigoHex.Substring(i, 2), 16);
                            memoriaFisica[dirFisica + (i / 2)] = valor;
                        }
                    }
                    // Si el tipo de registro = 'M' Entonces
                    else if (reg.StartsWith("M"))
                    {
                        int dirM = Convert.ToInt32(reg.Substring(1, 6), 16);
                        int longitudMediosBytes = Convert.ToInt32(reg.Substring(7, 2), 16);
                        char signo = reg[9];
                        string simbolo = reg.Substring(10).Trim();

                        // Busca el nombre del símbolo a modificar en TABSE
                        var elemento = TABSE.FirstOrDefault(t => t.Simbolo == simbolo || t.SeccionControl == simbolo);

                        if (elemento == null)
                        {
                            // Sino: Activa la bandera de error (símbolo externo indefinido)
                            throw new Exception($"Símbolo externo indefinido: {simbolo}");
                        }

                        // Si lo encuentra Entonces: Suma o Resta el valor del símbolo en la localidad (DIRSC + dirección especificada)
                        int dirFisicaMod = DIRSC + dirM;
                        ModificarMemoria(dirFisicaMod, longitudMediosBytes, signo, elemento.Direccion);
                    }
                    // Si se especifica una dirección en el registro de fin} Entonces
                    else if (reg.StartsWith("E"))
                    {
                        if (reg.Length > 1)
                        {
                            // Asigna DIREJ = DIRSC + dirección especificada
                            int dirE = Convert.ToInt32(reg.Substring(1, 6), 16);
                            DIREJ = DIRSC + dirE;
                        }
                    }
                }

                // Suma DIRSC = DIRSC + LONSC
                DIRSC += LONSC;
            }

            // Salta a la localidad dada por DIREJ {para iniciar la ejecución del programa cargado}
            // (Esto es simbólico en nuestra simulación, lo mostramos en la etiqueta al usuario)
        }

        private void ModificarMemoria(int dirBase, int mediosBytes, char signo, int valorSimbolo)
        {
            // Lectura con inicialización "perezosa" en -1 (FF)
            byte b1 = memoriaFisica.ContainsKey(dirBase) ? memoriaFisica[dirBase] : (byte)0xFF;
            byte b2 = memoriaFisica.ContainsKey(dirBase + 1) ? memoriaFisica[dirBase + 1] : (byte)0xFF;
            byte b3 = memoriaFisica.ContainsKey(dirBase + 2) ? memoriaFisica[dirBase + 2] : (byte)0xFF;

            int valorOriginal = (b1 << 16) | (b2 << 8) | b3;
            int valorModificado = 0;

            if (mediosBytes == 5)
            {
                int preservado = valorOriginal & 0xF00000;
                int aModificar = valorOriginal & 0x0FFFFF;

                if (signo == '+') aModificar += valorSimbolo;
                else if (signo == '-') aModificar -= valorSimbolo;

                valorModificado = preservado | (aModificar & 0x0FFFFF);

                // Para 5 medios bytes, el PDF respeta el Opcode. 
                // Solo marcamos de rojo el segundo y tercer byte.
                bytesModificados.Add(dirBase + 1);
                bytesModificados.Add(dirBase + 2);
            }
            else if (mediosBytes == 6)
            {
                int aModificar = valorOriginal & 0xFFFFFF;

                if (signo == '+') aModificar += valorSimbolo;
                else if (signo == '-') aModificar -= valorSimbolo;

                valorModificado = aModificar & 0xFFFFFF;

                // Para 6 medios bytes (Words/Datos), marcamos los 3 bytes de rojo.
                bytesModificados.Add(dirBase);
                bytesModificados.Add(dirBase + 1);
                bytesModificados.Add(dirBase + 2);
            }

            memoriaFisica[dirBase] = (byte)((valorModificado >> 16) & 0xFF);
            memoriaFisica[dirBase + 1] = (byte)((valorModificado >> 8) & 0xFF);
            memoriaFisica[dirBase + 2] = (byte)(valorModificado & 0xFF);
        }

        private void DibujarMapaMemoria()
        {
            dgvMemoria.Rows.Clear();

            if (memoriaFisica.Count == 0) return;

            int inicioVisual = minDirVisual - (minDirVisual % 16);
            int finVisual = maxDirVisual + (16 - (maxDirVisual % 16));

            bool ultimoFueVacio = false;

            for (int dir = inicioVisual; dir <= finVisual; dir += 16)
            {
                // Revisar si existe algún byte real cargado en esta fila (rango de 16 bytes)
                bool filaTieneDatos = false;
                for (int i = 0; i < 16; i++)
                {
                    if (memoriaFisica.ContainsKey(dir + i))
                    {
                        filaTieneDatos = true;
                        break;
                    }
                }

                if (!filaTieneDatos)
                {
                    if (!ultimoFueVacio)
                    {
                        dgvMemoria.Rows.Add("...");
                        ultimoFueVacio = true;
                    }
                    continue;
                }

                ultimoFueVacio = false;
                var row = new DataGridViewRow();
                row.CreateCells(dgvMemoria);
                row.Cells[0].Value = dir.ToString("X5");

                for (int i = 0; i < 16; i++)
                {
                    int dirFisica = dir + i;

                    if (memoriaFisica.ContainsKey(dirFisica))
                    {
                        row.Cells[i + 1].Value = memoriaFisica[dirFisica].ToString("X2");

                        if (bytesModificados.Contains(dirFisica))
                        {
                            row.Cells[i + 1].Style.ForeColor = Color.Red;
                            row.Cells[i + 1].Style.Font = new Font(dgvMemoria.Font, FontStyle.Bold);
                        }
                    }
                    else
                    {
                        row.Cells[i + 1].Value = "FF";
                        row.Cells[i + 1].Style.ForeColor = Color.DarkSlateGray;
                    }
                }

                dgvMemoria.Rows.Add(row);
            }
        }
    }

    public class ElementoTABSE
    {
        public string SeccionControl { get; set; } = "";
        public string Simbolo { get; set; } = "";
        public int Direccion { get; set; }
        public int Longitud { get; set; }
    }

    public class ArchivoObjeto
    {
        public string NombreArchivo { get; set; }
        public List<string> Registros { get; set; }
    }
}