using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoUpdaterDotNET;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Win32;
using NetMonitor.Properties;

namespace NetMonitor
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow
    {
        private bool _status = true;
        private int _nQuedas;
        private readonly Stopwatch _watchQueda = new Stopwatch();
        private int _intervalo = 5;

        private double _pingTabela;
        public SeriesCollection TabelaPingDataSource { get; set; }

        public MainWindow()
        {
            //CheckBoxStartup.IsChecked = Settings.Default.IniciarComWindows;

            InitializeComponent();
            CarregarConfigs();
            _watchQueda.Reset();
            Ticker();

            AutoUpdater.Start("https://raw.githubusercontent.com/Hyper1025/NetMonitor/master/NetMonitor/Updater.xml");

            //  Barra notificação
            Global.BarraNotifica = NotificaInferior;

            //  Log inicializado
            Logger.IniciarLog();
            Global.Notificar("Inicializado");

            //TimerIntevalo();

            // CODIGO NOVO
            TabelaPingDataSource = new SeriesCollection
            {
                new LineSeries
                {
                    // Dados iniciais da tabela
                    AreaLimit = -10,
                    Values = new ChartValues<ObservableValue>
                    {
                        new ObservableValue(0),
                        new ObservableValue(0),
                        new ObservableValue(0),
                        new ObservableValue(0),
                        new ObservableValue(0),
                        new ObservableValue(0),
                        new ObservableValue(0),
                        new ObservableValue(0),
                        new ObservableValue(0),
                        new ObservableValue(0),
                        new ObservableValue(0)
                    }
                }
            };

            Task.Run(AtualizarTabela);
            DataContext = this;
        }

        private Task AtualizarTabela()
        {
            while (true)
            {
                Thread.Sleep(_intervalo*1000);
                Application.Current.Dispatcher.Invoke(async () =>
                {
                    await IniciarTeste();
                    TabelaPingDataSource[0].Values.Add(new ObservableValue(_pingTabela));
                    TabelaPingDataSource[0].Values.RemoveAt(0);
                });
            }
        }

        //private Task AtualizarTabela()
        //{
        //    while (true)
        //    {
        //        Thread.Sleep(5000);
        //        Application.Current.Dispatcher.Invoke(() =>
        //        {
        //            IniciarTeste();
        //            TabelaPingDataSource[0].Values.Add(new ObservableValue(_pingTabela));
        //            TabelaPingDataSource[0].Values.RemoveAt(0);
        //        });
        //    }
        //}

        //  Um pequeno ticker pra loopar funções
        private void Ticker()
        {
            var watch = Stopwatch.StartNew();
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            t.Tick += delegate
            {
                TempoMonitorando.Text = $"{watch.Elapsed.Hours}:{watch.Elapsed.Minutes}:{watch.Elapsed.Seconds}";
                SaveConfigs();
                if (_watchQueda.IsRunning)
                {
                    TempoDeQueda.Text = $"{_watchQueda.Elapsed.Hours}:{_watchQueda.Elapsed.Minutes}:{_watchQueda.Elapsed.Seconds}";
                }
            };
            t.Start();
        }

        private void SaveConfigs()
        {
            _intervalo = (int)SliderIntervalo.Value;
            TextBlockIntervalo.Text = $"{SliderIntervalo.Value} s";
            Settings.Default.Intevalo = (int)SliderIntervalo.Value;
            if (CheckBoxStartup.IsChecked.HasValue)
            {
                Settings.Default.IniciarComWindows = CheckBoxStartup.IsChecked.Value;
            }
            Settings.Default.Save();
        }

        private void CarregarConfigs()
        {
            CheckBoxPing.IsChecked = Settings.Default.PingCheckbox;
            CheckBoxStartup.IsChecked = Settings.Default.IniciarComWindows;
            SliderIntervalo.Value = Settings.Default.Intevalo;
        }

        //  Timer que loopa o intervalo definido pelo usuário
        //private void TimerIntevalo()
        //{
        //    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        //    timer.Tick += delegate
        //    {
        //        IniciarTeste();
        //        //timer.Stop();
        //    };
        //    timer.Start();
        //}

        //  Função que realiza a chamada do teste e valida o status de rede

        private async Task IniciarTeste()
        {
            // Testa os 3 servidores
            EstruturaDeResposta test1 = default;
            EstruturaDeResposta test2 = default;
            EstruturaDeResposta test3 = default;
            EstruturaDeResposta test4 = default;
            
            var p = new Ping();
            var buffer = new byte[32];
            var timeout = 1000;
            var pingOptions = new PingOptions();

            string[] listaIps = {"8.8.8.8", "208.67.222.222", "1.1.1.1", "104.160.152.3"};
            foreach (var ip in listaIps)
            {
                PingReply r = null;

                try
                {
                    r = await p.SendPingAsync(ip, timeout, buffer, pingOptions);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                var retorno = new EstruturaDeResposta();

                if (r== null)
                {
                    switch (ip)
                    {
                        case "8.8.8.8":
                            test1.Sucesso = false;
                            test1.TempoDeResposta = 1000;
                            IconPingGoogle.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            GoogleMs.Text = "Sem rede";
                            break;
                        case "208.67.222.222":
                            test2.Sucesso = false;
                            test2.TempoDeResposta = 1000;
                            IconPingOpenDns.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            OpenDnsMs.Text = "Sem rede";
                            break;
                        case "1.1.1.1":
                            test3.Sucesso = false;
                            test3.TempoDeResposta = 1000;
                            IconPingCloudflare.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            CloudflareMs.Text = "Sem rede";
                            break;
                        case "104.160.152.3":
                            test4.Sucesso = false;
                            test4.TempoDeResposta = 1000;
                            IconPingGame.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            GameMs.Text = "Sem rede";
                            break;
                    }
                    break;
                }

                if (r.Status == IPStatus.Success)
                {
                    retorno.Sucesso = true;
                    retorno.TempoDeResposta = r.RoundtripTime;
                }
                else
                {
                    retorno.Sucesso = false;
                    retorno.TempoDeResposta = 1000;
                }

                switch (ip)
                {
                    case "8.8.8.8":
                        test1 = retorno;
                        if (retorno.Sucesso)
                        {
                            // Exibe o resultado positivo e coloca a cor referente ao ping
                            IconPingGoogle.Foreground = retorno.TempoDeResposta > 80 ? Global.HexToColorBrushConverter("#ffb229") : Global.HexToColorBrushConverter("#4ad295");
                            GoogleMs.Text = $"{retorno.TempoDeResposta} ms";
                        }
                        else
                        {
                            IconPingGoogle.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            GoogleMs.Text = "Sem rede";
                        }
                        break;
                    case "208.67.222.222":
                        test2 = retorno;
                        if (retorno.Sucesso)
                        {
                            // Exibe o resultado positivo e coloca a cor referente ao ping
                            IconPingOpenDns.Foreground = retorno.TempoDeResposta > 80 ? Global.HexToColorBrushConverter("#ffb229") : Global.HexToColorBrushConverter("#4ad295");
                            OpenDnsMs.Text = $"{retorno.TempoDeResposta} ms";
                        }
                        else
                        {
                            IconPingOpenDns.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            OpenDnsMs.Text = "Sem rede";
                        }
                        break;
                    case "1.1.1.1":
                        test3 = retorno;
                        if (retorno.Sucesso)
                        {
                            // Exibe o resultado positivo e coloca a cor referente ao ping
                            IconPingCloudflare.Foreground = retorno.TempoDeResposta > 80 ? Global.HexToColorBrushConverter("#ffb229") : Global.HexToColorBrushConverter("#4ad295");
                            CloudflareMs.Text = $"{retorno.TempoDeResposta} ms";
                        }
                        else
                        {
                            IconPingCloudflare.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            CloudflareMs.Text = "Sem rede";
                        }
                        break;
                    case "104.160.152.3":
                        test4 = retorno;
                        if (retorno.Sucesso)
                        {
                            // Exibe o resultado positivo e coloca a cor referente ao ping
                            IconPingGame.Foreground = retorno.TempoDeResposta > 80 ? Global.HexToColorBrushConverter("#ffb229") : Global.HexToColorBrushConverter("#4ad295");
                            GameMs.Text = $"{retorno.TempoDeResposta} ms";
                        }
                        else
                        {
                            IconPingGame.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            GameMs.Text = "Sem rede";
                        }
                        break;
                }
            }


            // Verificamos o resultados 
            // REDE OFF
            if (test1.Sucesso == false && test2.Sucesso == false && test3.Sucesso == false && test4.Sucesso == false)
            {
                if (_status)
                {
                    // Constata queda
                    Logger.Escrever(Logger.LogType.Queda);
                    Global.Notificar("Sua rede caiu", "ff0000");

                    HorarioDaQueda.Text = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                    _nQuedas += 1;
                    _status = false;
                    ContadorDeQuedas.Text = _nQuedas.ToString();
                    _watchQueda.Start();
                }
            }

            // REDE ON
            if (test1.Sucesso || test2.Sucesso || test3.Sucesso || test4.Sucesso)
            {
                if (_status == false)
                {
                    Logger.Escrever(Logger.LogType.Retomada);
                    Global.Notificar("Conexão retomada");
                    HorarioDeRetorno.Text = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                    _status = true;
                    _watchQueda.Reset();
                }
            }

            if (test4.Sucesso)
            {
                _pingTabela = test4.TempoDeResposta;
            }
            else
            {
                _pingTabela = 1000;
            }

            // Ping Jogo/Todos servidores
            if (CheckBoxPing.IsChecked.HasValue)
            {
                if (test4.TempoDeResposta > 80)
                {
                    if (_status)
                    {
                        PingAlto.Text = $"{DateTime.Now.ToString(CultureInfo.CurrentCulture)} | {test4.TempoDeResposta} ms";
                        Logger.Escrever(Logger.LogType.Latencia);
                        Global.Notificar("Ping alto servidor de jogo", "ff0000");
                    }
                }
            }
            else
            {
                if (test1.TempoDeResposta > 80 && test2.TempoDeResposta > 80 && test3.TempoDeResposta > 80 && test4.TempoDeResposta > 80)
                {
                    if (_status)
                    {
                        PingAlto.Text = $"{DateTime.Now.ToString(CultureInfo.CurrentCulture)} | {test4.TempoDeResposta} ms";
                        Logger.Escrever(Logger.LogType.Latencia);
                        Global.Notificar("Ping alto servidor de jogo", "ff0000");
                    }
                }
            }

            // Restart do ciclo
            //TimerIntevalo();
        }

        // O nome já diz, é uma pequena estrutura
        // responsável por organizar a resposta
        // que é dada pelo PingTest
        private struct EstruturaDeResposta
        {
            private long? _tempoDeResposta;
            bool? _sucesso;

            public long TempoDeResposta { get => _tempoDeResposta ?? 100;
                set => _tempoDeResposta = value;
            }
            public bool Sucesso { get => _sucesso ?? false;
                set => _sucesso = value;
            }

        }


        // Função responsável por realizar o teste
        //private EstruturaDeResposta PingTest(string ip, TextBlock textBlock, PackIcon packIcon)
        //{
        //    var ping = new Ping();
        //    var host = ip;
        //    var buffer = new byte[32];
        //    var timeout = 1000;
        //    var pingOptions = new PingOptions();
        //    PingReply reply;

        //    var resposta = new EstruturaDeResposta();

        //    try
        //    {
        //        reply = ping.Send(host, timeout, buffer, pingOptions);
        //    }
        //    catch (Exception)
        //    {
        //        reply = null;
        //    }

        //    //  Resultado positivo
        //    if (reply != null && reply.Status == IPStatus.Success)
        //    {
        //        // Exibe o resultado positivo e coloca a cor referente ao ping
        //        packIcon.Foreground = reply.RoundtripTime > 80 ? Global.HexToColorBrushConverter("#ffb229") : Global.HexToColorBrushConverter("#4ad295");

        //        textBlock.Text = $"{reply.RoundtripTime} ms";
        //        ping.Dispose();

        //        resposta.Sucesso = true;
        //        resposta.TempoDeResposta = reply.RoundtripTime;
        //        return resposta;
        //    }

        //    // Resultado negativo
        //    // Exibe o resultado negativo e mostra a cor vermelha
        //    packIcon.Foreground = Global.HexToColorBrushConverter("#ff0000");
        //    textBlock.Text = reply == null ? "Sem rede" : reply.Status.ToString();
        //    ping.Dispose();

        //    resposta.Sucesso = false;
        //    resposta.TempoDeResposta = 1000;
        //    return resposta;
        //}

        // Botão minimizar
        private void ButtonMinimizar_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // Botão fechar
        private void ButtonFechar_OnClick(object sender, RoutedEventArgs e)
        {
            Logger.Escrever(Logger.LogType.Final,TempoMonitorando.Text,ContadorDeQuedas.Text);
            Application.Current.Shutdown();
        }

        // Evento para mover a janela
        private void Rectangle_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Logger._pasta);
        }
        
        //  Adiciona e remove do startup

        //private void startup()
        //{
        //    RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        //    if ()
        //    {
        //        rk?.SetValue("NetMonitor", System.Reflection.Assembly.GetExecutingAssembly().Location);
        //    }
        //    else
        //    {
        //        rk?.DeleteValue("NetMonitor", false);
        //    }
        //}
        private void CheckBoxStartup_OnChecked(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.SetValue("NetMonitor", System.Reflection.Assembly.GetExecutingAssembly().Location);
            rk.Close();
        }

        private void CheckBoxStartup_OnUnchecked(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.DeleteValue("NetMonitor", false);
            rk.Close();
        }
    }
}
