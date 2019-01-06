using System;
using System.CodeDom.Compiler;
using System.Text;
using System.Windows;


namespace Generator_analizatoare_LL1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Load();
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message);
            }
        }

        private void Load()
        {
            Grammar grammar = new Grammar();
            grammar.ReadFile(pathBox.Text);

            CodeGenerator generator = new CodeGenerator
            {
                Grammar = new LL1Grammar(grammar)
            };

            codeBlock.Text = (generator.GetCode());
        }

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Run();
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message);
            }
        }

        private void Run()
        {
            var provider = CodeDomProvider.CreateProvider("CSharp");
            CompilerParameters cp = new CompilerParameters
            {
                GenerateExecutable = true,
                GenerateInMemory = false,
                TreatWarningsAsErrors = false
            };
            cp.ReferencedAssemblies.Add("System.Text.RegularExpressions.dll");
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("netstandard.dll");
            cp.ReferencedAssemblies.Add("System.Linq.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");

            // Invoke compilation of the source file.
            CompilerResults cr = provider.CompileAssemblyFromSource(cp, new[] { codeBlock.Text });
            if (cr.Errors.Count > 0)
            {
                // Display compilation errors.
                StringBuilder errors = new StringBuilder($"{cr.Errors.Count} errors:\n");
                foreach (CompilerError ce in cr.Errors)
                {
                    errors.AppendFormat("\n\n{0}", ce.ErrorText);
                }
                MessageBox.Show(errors.ToString());
            }
            else
            {
                System.Diagnostics.Process.Start(cr.PathToAssembly);
            }
        }
    }
}
