using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;


namespace ProyectoSoftwareSistemas
{
    class ParserErrorListener : BaseErrorListener
    {
        public override void SyntaxError(
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"X Error sintáctico en línea {line}, columna {charPositionInLine}");
            Console.WriteLine($"   Token: {offendingSymbol?.Text}");
            Console.WriteLine($"   Detalle: {msg}");
            Console.ResetColor();
        }
    }

    class LexerErrorListener : IAntlrErrorListener<int>
    {
        public void SyntaxError(
            IRecognizer recognizer,
            int offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"X Error léxico en línea {line}, columna {charPositionInLine}");
            Console.WriteLine($"   Detalle: {msg}");
            Console.ResetColor();
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Ingresa el nombre del archivo (.txt o .asm)");
                Console.WriteLine("Escribe 'salir' para terminar:");
                Console.Write("> ");

                string nombreArchivo = Console.ReadLine();

                if (nombreArchivo.Equals("salir", StringComparison.OrdinalIgnoreCase))
                    break;

                string ruta = Path.Combine("input", nombreArchivo);

                if (!File.Exists(ruta))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("x_x El archivo no existe en la carpeta 'input'.");
                    Console.ResetColor();
                    Console.ReadKey();
                    continue;
                }

                string contenido = File.ReadAllText(ruta);

                Console.WriteLine("\nLectura del archivo: ");
                Console.WriteLine(contenido);

                Console.WriteLine("\nSalida del arbol: ");

                try
                {
                    // 1. Input
                    AntlrInputStream inputStream = new AntlrInputStream(contenido);

                    // 2. Lexer
                    SICXELexer lexer = new SICXELexer(inputStream);
                    lexer.RemoveErrorListeners();
                    lexer.AddErrorListener(new LexerErrorListener());

                    // 3. Tokens
                    CommonTokenStream tokens = new CommonTokenStream(lexer);

                    // 4. Parser
                    SICXEParser parser = new SICXEParser(tokens);
                    parser.RemoveErrorListeners();
                    parser.AddErrorListener(new ParserErrorListener());
                    parser.BuildParseTree = true;

                    // 5. Regla inicial
                    IParseTree tree = parser.program();

                    // 6. Mostrar árbol
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(tree.ToStringTree(parser));
                    Console.ResetColor();

                    var root = tree as SICXEParser.ProgramContext;

                    var generador = new GeneradorArchivoIntermedio(root);

                    var lineas = generador.GenerarLineas();

                    generador.GenerarExcel(lineas, nombreArchivo);

                    Console.WriteLine("\n\n\n\n****** Archivo intermedio generado ******");
                    generador.ImprimirTABSIM();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("x_x Error fatal durante el análisis:");
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }

                Console.WriteLine("\nPresiona cualquier tecla para volver a iniciar...");
                Console.ReadKey();
            }
        }
    }
}
