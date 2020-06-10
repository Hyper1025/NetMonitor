using System;
using System.IO;

namespace NetMonitor
{
    internal class Logger
    {
        public static string _path;
        public static string _pasta;


        internal static void IniciarLog()
        {
            var pasta = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/NetMonitor/Logs";
            var nomeArquivo = $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}-T{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}-{DateTime.Now.Millisecond}";
            var arquivo = $"{pasta}/{nomeArquivo}.txt";

            _path = arquivo;
            _pasta = pasta;

            //  Verifica existência da pasta de log
            if (!Directory.Exists(pasta))
            {
                //  Cria a pasta 
                Directory.CreateDirectory(pasta);
            }

            //  Cria o arquivo
            File.WriteAllText(arquivo, $@"======Log De Falhas - NetMonitor=======
=======================================
{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day} {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}	Inicializado
---------------------------------------");

            //  Deleta arquivos mais antigos (3 meses)
            string[] files = Directory.GetFiles(pasta);

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.LastAccessTime < DateTime.Now.AddMonths(-3))
                    fi.Delete();
            }
        }


        internal static void Escrever(LogType logType, string TempoMonitorando = null, string numeroFalhas = null)
        {
            
            var linha = "";

            switch (logType)
            {
                case LogType.Queda:
                    linha = $"\n{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day} {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}\tConexão Perdida\r";
                    break;
                case LogType.Retomada:
                    linha = $"\n{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day} {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}\tConexão Retomada\r";
                    break;
                case LogType.Latencia:
                    linha= $"\n{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day} {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}\tAlta Latência\r";
                    break;
                case LogType.Final:
                    linha =
                        $"\n---------------------------------------\r\n{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day} {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}\tFinal Do Log\r\nDuratção:\t\t\t{TempoMonitorando}\r\nNúmero de falhas:\t{numeroFalhas}\r\n=======================================";
                    break;
            }

            string logRead = File.ReadAllText(_path);
            File.WriteAllText(_path, logRead+linha);

            if (File.Exists(_path))
            {
                
            }
        }

        internal enum LogType
        {
            Queda,
            Retomada,
            Latencia,
            Final
        }
    }
}