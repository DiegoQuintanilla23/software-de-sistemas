using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System.Drawing;

namespace ProyectoSoftwareSistemas
{
    public partial class InterfazSicXE : Form
    {
        private string rutaArchivoActual = string.Empty;
        private Dictionary<string, Color> coloresSeccion = new Dictionary<string, Color>();


        public InterfazSicXE()
        {
            InitializeComponent();
            ConfigurarDialogos();
        }

        private void ConfigurarDialogos()
        {
            openFileDialog1.Filter = "Archivos de ensamblador (*.asm, *.txt)|*.asm;*.txt|Todos los archivos (*.*)|*.*";
            saveFileDialog1.Filter = "Archivos de ensamblador (*.asm, *.txt)|*.asm;*.txt";
        }

        #region Eventos del Menú

        private void nuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Verificar si hay texto antes de borrar
            if (!string.IsNullOrEmpty(txtEditor.Text))
            {
                DialogResult resultado = MessageBox.Show(
                    "¿Deseas crear un nuevo archivo? Se perderán los cambios no guardados.",
                    "Nuevo archivo",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (resultado == DialogResult.No)
                {
                    return;
                }
            }

            //Limpiar el editor de texto
            txtEditor.Clear();

            //Reiniciar la ruta del archivo actual
            rutaArchivoActual = string.Empty;

            //Actualizar el título de la ventana
            this.Text = "SicXeWorkspace - Nuevo archivo";

            //Limpiar los DataGridViews para que no queden datos del proyecto anterior
            dgvIntermedio.DataSource = null;
            dgvTabsim.DataSource = null;
            dgvBloques.DataSource = null;
            dgvObjeto.DataSource = null;

            txtEditor.Focus();
        }

        private void guardarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rutaArchivoActual))
            {
                guardarComoToolStripMenuItem_Click(sender, e);
            }
            else
            {
                File.WriteAllText(rutaArchivoActual, txtEditor.Text);
                MessageBox.Show("Archivo guardado con éxito.", "Guardar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void guardarComoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                rutaArchivoActual = saveFileDialog1.FileName;
                File.WriteAllText(rutaArchivoActual, txtEditor.Text);
                this.Text = $"InterfazSicXE - {Path.GetFileName(rutaArchivoActual)}";
            }
        }

        private void ensamblarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EjecutarProcesoEnsamblado();
        }

        #endregion

        private void EjecutarProcesoEnsamblado()
        {
            if (string.IsNullOrWhiteSpace(txtEditor.Text))
            {
                MessageBox.Show("No hay código para ensamblar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                dgvIntermedio.DataSource = null;
                dgvTabsim.DataSource = null;
                dgvBloques.DataSource = null;
                dgvObjeto.DataSource = null;

                //Configuración de ANTLR
                AntlrInputStream inputStream = new AntlrInputStream(txtEditor.Text);
                SICXELexer lexer = new SICXELexer(inputStream);

                var lexerErrorListener = new LexerErrorListener();
                lexer.RemoveErrorListeners();
                lexer.AddErrorListener(lexerErrorListener);

                CommonTokenStream tokens = new CommonTokenStream(lexer);
                SICXEParser parser = new SICXEParser(tokens);

                var parserErrorListener = new ParserErrorListener();
                parser.RemoveErrorListeners();
                parser.AddErrorListener(parserErrorListener);
                parser.BuildParseTree = true;

                //Generación del árbol sintáctico
                IParseTree tree = parser.program();
                var root = tree as SICXEParser.ProgramContext;

                // --- PASADA 1: Direcciones y Símbolos ---
                var generadorIntermedio = new GeneradorArchivoIntermedio(root);
                List<LineaIntermedia> lineas = generadorIntermedio.GenerarLineas();
                // AQUI SE OBTIENEN LAS SECCIONES, QUE CONTIENE CADA UNA SUS PROPIAS TABLAS
                List<Seccion> secciones = generadorIntermedio.GetSecciones();
                // SE TOMAN LAS PRIMERAS TABLAS POR AHORA
                Dictionary<string, Simbolo> tabsim = secciones[0].TABSIM;
                Dictionary<string, Bloque> tabblk = secciones[0].TABBLK;

                // --- PASADA 2: Código Objeto ---
                var generadorCodigoObjeto = new GeneradorCodigoObjeto(tabsim, lineas, tabblk);
                generadorCodigoObjeto.Generar();

                // --- PASADA 3: Registros del Programa Objeto ---
                var generadorProgramaObjeto = new GeneradorProgramaObjeto(lineas, tabsim, tabblk);
                List<string> archivoObjeto = generadorProgramaObjeto.Generar();

                // 3. Cargar datos en los DataGridViews
                //Cargar datos en los DataGridViews
                ActualizarTablas(lineas, secciones, archivoObjeto);

                //Generar el Excel (solo si el archivo ya fue guardado y tiene ruta)
                if (!string.IsNullOrEmpty(rutaArchivoActual))
                {
                    generadorIntermedio.GenerarExcel(lineas, archivoObjeto, Path.GetFileName(rutaArchivoActual));
                }

                //Gestión de Errores de ANTLR
                var todosLosErrores = lexerErrorListener.Errores.Concat(parserErrorListener.Errores).ToList();

                if (todosLosErrores.Count > 0)
                {
                    string mensajeErrores = string.Join("\n", todosLosErrores.Take(10));
                    if (todosLosErrores.Count > 10) mensajeErrores += "\n... y más errores.";

                    MessageBox.Show($"Se completó el ensamblado pero se detectaron errores léxicos/sintácticos:\n\n{mensajeErrores}",
                        "Ensamblado con Errores", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("Ensamblado completado con éxito sin errores de sintaxis.", "SIC/XE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico en la ejecución:\n{ex.Message}", "Error Fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarTablas(List<LineaIntermedia> lineas, List<Seccion> secciones, List<string> objeto)
        {
            // 1. Archivo Intermedio
            dgvIntermedio.DataSource = lineas;
            if (dgvIntermedio.Columns["ContadorPrograma"] != null)
            {
                dgvIntermedio.Columns["ContadorPrograma"].HeaderText = "P.C.";
            }

            foreach (DataGridViewRow row in dgvIntermedio.Rows)
            {
                if (row.Cells["NumeroBloque"].Value != null)
                {
                    int b = Convert.ToInt32(row.Cells["NumeroBloque"].Value);

                    Color color = Color.FromArgb(
                        230 + (b * 10) % 25,
                        230 + (b * 20) % 25,
                        230 + (b * 30) % 25
                    );

                    row.DefaultCellStyle.BackColor = color;
                }

                if (row.Cells["CodigoOp"].Value != null &&
                    row.Cells["CodigoOp"].Value.ToString() == "CSECT")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(200, 200, 255);
                    row.DefaultCellStyle.Font = new Font(dgvIntermedio.Font, FontStyle.Bold);
                }
            }

            // 2. TABSIM
            var todosSimbolos = secciones.SelectMany(sec => sec.TABSIM.Values.Select(s => new {
                Seccion = sec.Nombre,
                Simbolo = s.Nombre,
                Direccion = s.Direccion,
                Tipo = s.Tipo,
                NumBloq = s.Bloque,
                SimboloExterno = (s.Tipo == "E")
            })).ToList();

            dgvTabsim.DataSource = todosSimbolos;
            if (dgvTabsim.Columns["Direccion"] != null)
            {
                dgvTabsim.Columns["Direccion"].DefaultCellStyle.Format = "X4";
            }

            ColorearPorSeccion(dgvTabsim, "Seccion");

            // 3. Tabla de Bloques
            var todosBloques = secciones.SelectMany(sec => sec.TABBLK.Values.Select(b => new {
                Seccion = sec.Nombre,
                Numero = b.Numero,
                Nombre = b.Nombre,
                Locctr = b.Locctr,
                Inicio = b.DirInicial,
                Longitud = b.Longitud
            })).ToList();

            dgvBloques.DataSource = todosBloques;
            if (dgvBloques.Columns["Locctr"] != null)
            {
                dgvBloques.Columns["Locctr"].DefaultCellStyle.Format = "X4";
            }
            if (dgvBloques.Columns["Inicio"] != null)
            {
                dgvBloques.Columns["Inicio"].DefaultCellStyle.Format = "X4";
            }
            if (dgvBloques.Columns["Longitud"] != null)
            {
                dgvBloques.Columns["Longitud"].DefaultCellStyle.Format = "X4";
            }

            ColorearPorSeccion(dgvBloques, "Seccion");

            // 4. Programa Objeto
            dgvObjeto.DataSource = objeto.Select(reg => new { Registro = reg }).ToList();

            FormatearGrids();
        }

        private void FormatearGrids()
        {
            DataGridView[] grids = { dgvIntermedio, dgvTabsim, dgvBloques, dgvObjeto };
            foreach (var grid in grids)
            {
                if (grid != null && grid.DataSource != null)
                {
                    grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    grid.ReadOnly = true;
                    grid.AllowUserToAddRows = false;
                    grid.RowHeadersVisible = false;
                }
            }
        }

        private Color ObtenerColorSeccion(string seccion)
        {
            if (!coloresSeccion.ContainsKey(seccion))
            {
                var coloresPastel = new List<Color>
        {
            Color.FromArgb(255, 230, 230),
            Color.FromArgb(230, 255, 230),
            Color.FromArgb(230, 230, 255),
            Color.FromArgb(255, 255, 230),
            Color.FromArgb(255, 230, 255),
            Color.FromArgb(230, 255, 255)
        };

                coloresSeccion[seccion] = coloresPastel[coloresSeccion.Count % coloresPastel.Count];
            }

            return coloresSeccion[seccion];
        }

        private void ColorearPorSeccion(DataGridView grid, string columnaSeccion)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells[columnaSeccion].Value != null)
                {
                    string sec = row.Cells[columnaSeccion].Value.ToString();
                    row.DefaultCellStyle.BackColor = ObtenerColorSeccion(sec);
                }
            }
        }

        private void abrirToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                rutaArchivoActual = openFileDialog1.FileName;
                txtEditor.Text = File.ReadAllText(rutaArchivoActual);
                this.Text = $"InterfazSicXE - {Path.GetFileName(rutaArchivoActual)}";
            }
        }

    }

    public class ParserErrorListener : BaseErrorListener
    {
        public List<string> Errores { get; } = new List<string>();

        public override void SyntaxError(
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            Errores.Add($"Error sintáctico en línea {line}, columna {charPositionInLine} (Token: {offendingSymbol?.Text}): {msg}");
        }
    }

    public class LexerErrorListener : IAntlrErrorListener<int>
    {
        public List<string> Errores { get; } = new List<string>();

        public void SyntaxError(
            IRecognizer recognizer,
            int offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            Errores.Add($"Error léxico en línea {line}, columna {charPositionInLine}: {msg}");
        }
    }
}