using CommandLine;

namespace ReportCLI
{
    public class Options
    {
        [Option('p', "path", Required = true, HelpText = "Path to test results")]
        public string Path { get; set; }
        
        [Option("registrator", Required = false, HelpText = "Registrator position. Default value = 0")]
        public double? RegistratorPosition { get; set; }

        [Option('t', "time", Required = false, HelpText = "Default time = 0")]
        public double? Time { get; set; }

        [Option('r', "reports", Required = true, HelpText = "Set number of option to run selected reports")]
        public int Reports { get; set; }

        [Option('s', "savePath", Required = true, HelpText = "Path to saved charts")]
        public string PathToSave { get; set; }
    }
}