using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using AutoUpdaterDotNET;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Win32;
using NetMonitor.Properties;
using Application = System.Windows.Application;

namespace NetMonitor
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow
    {
        private bool inicializando = true;
        private bool _status = true;
        private int _nQuedas;
        private Stopwatch _watchQueda = new Stopwatch();
        private int _intervalo = 5;
        private double _pingTabela;
        public SeriesCollection TabelaPingDataSource { get; set; }

        public NotifyIcon NotifyIcon = new NotifyIcon();

        public MainWindow()
        {
            //  Verifica se deve iniciar minimizado
            if (Settings.Default.InicializarMinimizado)
            {
                WindowState = WindowState.Minimized;
                Hide();
            }

            //  Verifica se existe uma configuração de posição
            //  Se existir, joga o programa pra posição salva
            if (Settings.Default.PosX != 0 && Settings.Default.PosY != 0)
            {
                Left = Settings.Default.PosX;
                Top = Settings.Default.PosY;
            }

            //  Inicializa os componentes
            InitializeComponent();

            //  Carrega as configurações
            CarregarConfigs();

            //  Como o relógio de queda já inicia quando é criado
            //  Resetamos ele, pra ter seu valor 0 desde o começo
            _watchQueda.Reset();

            //  Ativamos o nosso ticker
            //  Ele é responsável por realizar as ações consecutivamente
            //  Como atualizar os timers da interface e manter as configurções atualizadas :)
            Ticker();

            //  Cria o icone bandeja windows
            NotifyIcon.Icon = Properties.Resources.Internet_256;
            NotifyIcon.Visible = true;
            NotifyIcon.Text = @"NetMonitor";
            NotifyIcon.DoubleClick += NotifyIcon_RetomarJanela;

            //  Cria o menu do ícone da bandeja
            var contextMenu = new ContextMenu();

            //  Botão mostrar do ícone da bandeja
            //  Botão minimizar para a bandeja do icone da bandeja
            //  Botão abrir pasta de log
            //  Botão de sair do icone da bandeja
            contextMenu.MenuItems.Add(new MenuItem("Mostrar",NotifyIcon_RetomarJanela));
            contextMenu.MenuItems.Add(new MenuItem(@"Minimizar para a bandeja",
                delegate { WindowState = WindowState.Minimized;}));
            contextMenu.MenuItems.Add(new MenuItem(@"Abrir pasta de log", 
                delegate { Process.Start(Logger._pasta); }));
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(new MenuItem(@"Sair", delegate { Application.Current.Shutdown(); }));
            
            //  Passa o ContextMenu para o NotifyIcon
            NotifyIcon.ContextMenu = contextMenu;

            //  Verifica update
            AutoUpdater.Start("https://raw.githubusercontent.com/Hyper1025/NetMonitor/master/NetMonitor/Updater.xml");


            //  Cria a coleção de itens iniciais que vão aparecer na planilha de ping
            TabelaPingDataSource = new SeriesCollection
            {
                new LineSeries
                {
                    // Dados iniciais da tabela
                    AreaLimit = -20,
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
                        new ObservableValue(0),
                        new ObservableValue(0)
                    }
                }
            };

            //  Barra notificação
            Global.BarraNotifica = NotificaInferior;

            //  Log inicializado
            Logger.IniciarLog();
            if (!Settings.Default.InicializarMinimizado)
            {
                Global.Notificar("Inicializado");
            }

            Task.Run(AtualizarTabela);
            DataContext = this;

            inicializando = false;
        }

        //  Clique duplo no icone da bandeja
        private void NotifyIcon_RetomarJanela(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                Hide();
            base.OnStateChanged(e);
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

        //  Salva as configurações
        private void SaveConfigs()
        {
            _intervalo = (int)SliderIntervalo.Value;
            TextBlockIntervalo.Text = $"{SliderIntervalo.Value} s";
            Settings.Default.Intevalo = (int)SliderIntervalo.Value;
            if (CheckBoxStartup.IsChecked.HasValue)
            {
                Settings.Default.IniciarComWindows = CheckBoxStartup.IsChecked.Value;
            }

            if (CheckBoxStartupMinimizado.IsChecked.HasValue)
            {
                Settings.Default.InicializarMinimizado = CheckBoxStartupMinimizado.IsChecked.Value;
            }

            Settings.Default.PosX = Left;
            Settings.Default.PosY = Top;

            Settings.Default.Save();
        }

        //  Carrega as configurações
        private void CarregarConfigs()
        {
            CheckBoxStartupMinimizado.IsChecked = Settings.Default.InicializarMinimizado;
            CheckBoxPing.IsChecked = Settings.Default.PingCheckbox;
            CheckBoxStartup.IsChecked = Settings.Default.IniciarComWindows;
            SliderIntervalo.Value = Settings.Default.Intevalo;
        }

        //  Função que realiza a chamada do teste e valida o status de rede

        private async Task IniciarTeste()
        {
            // Testa os 3 servidores
            EstruturaDeResposta respostaStatusGoogle = default;
            EstruturaDeResposta respostaStatusOpenDns = default;
            EstruturaDeResposta respostaStatusCloudFlare = default;
            EstruturaDeResposta respostaStatusRiot = default;

            var p = new Ping();
            var buffer = new byte[32];
            const int timeout = 1000;
            var pingOptions = new PingOptions();

            string[] listaIps = { "8.8.8.8", "208.67.222.222", "1.1.1.1", "104.160.152.3" };
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

                //var VariavelDoLoopInternoParaRetorno = new EstruturaDeResposta();

                //  Verificamos se 'r' é nulo
                if (r == null)
                {
                    //  Se for nulo, verificamos o ip de entrada
                    //  e passamos o valor para a respectiva estrutura de resposta
                    //  Primeiro descarte é feito se a resposta for nula
                    switch (ip)
                    {
                        //  Resposta Google
                        case "8.8.8.8":
                            respostaStatusGoogle.Sucesso = false;
                            respostaStatusGoogle.TempoDeResposta = 1000;
                            IconPingGoogle.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            GoogleMs.Text = "Sem rede";
                            break;
                        //  Resposta OpenDns
                        case "208.67.222.222":
                            respostaStatusOpenDns.Sucesso = false;
                            respostaStatusOpenDns.TempoDeResposta = 1000;
                            IconPingOpenDns.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            OpenDnsMs.Text = "Sem rede";
                            break;
                        //  Resposta CloudFlare
                        case "1.1.1.1":
                            respostaStatusCloudFlare.Sucesso = false;
                            respostaStatusCloudFlare.TempoDeResposta = 1000;
                            IconPingCloudflare.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            CloudflareMs.Text = "Sem rede";
                            break;
                        //  Resposta Riot
                        case "104.160.152.3":
                            respostaStatusRiot.Sucesso = false;
                            respostaStatusRiot.TempoDeResposta = 1000;
                            IconPingGame.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            _pingTabela = 1000;
                            GameMs.Text = "Sem rede";
                            break;
                    }
                    //break;
                }
                //  Do contrário verficamos se obtivemos sucesso
                //  Agora verificamos se obtivemos sucesso ao conectar com o ip
                else if (r.Status == IPStatus.Success)
                {
                    switch (ip)
                    {
                        //  Resposta Google
                        case "8.8.8.8":
                            respostaStatusGoogle.Sucesso = true;
                            respostaStatusGoogle.TempoDeResposta = r.RoundtripTime;
                            //  Atribuimos a cor do icone de acordo com o ping.
                            //  Caso ping > 80, recebe a cor amarela
                            //  Caso ping < 80, recebe  a cor verde verde
                            IconPingGoogle.Foreground = r.RoundtripTime > 80 ? Global.HexToColorBrushConverter("#ffb229") : Global.HexToColorBrushConverter("#4ad295");
                            GoogleMs.Text = $"{r.RoundtripTime} ms";
                            break;
                        //  Resposta OpenDns
                        case "208.67.222.222":
                            respostaStatusOpenDns.Sucesso = true;
                            respostaStatusOpenDns.TempoDeResposta = r.RoundtripTime;
                            //  Atribuimos a cor do icone de acordo com o ping.
                            //  Caso ping > 80, recebe a cor amarela
                            //  Caso ping < 80, recebe  a cor verde verde
                            IconPingOpenDns.Foreground = r.RoundtripTime > 80 ? Global.HexToColorBrushConverter("#ffb229") : Global.HexToColorBrushConverter("#4ad295");
                            OpenDnsMs.Text = $"{r.RoundtripTime} ms";
                            break;
                        //  Resposta CloudFlare
                        case "1.1.1.1":
                            respostaStatusCloudFlare.Sucesso = true;
                            respostaStatusCloudFlare.TempoDeResposta = r.RoundtripTime;
                            //  Atribuimos a cor do icone de acordo com o ping.
                            //  Caso ping > 80, recebe a cor amarela
                            //  Caso ping < 80, recebe  a cor verde verde
                            IconPingCloudflare.Foreground = r.RoundtripTime > 80 ? Global.HexToColorBrushConverter("#ffb229") : Global.HexToColorBrushConverter("#4ad295");
                            CloudflareMs.Text = $"{r.RoundtripTime} ms";
                            break;
                        //  Resposta Riot
                        case "104.160.152.3":
                            respostaStatusRiot.Sucesso = true;
                            respostaStatusRiot.TempoDeResposta = r.RoundtripTime;
                            //  Atribuimos a cor do icone de acordo com o ping.
                            //  Caso ping > 80, recebe a cor amarela
                            //  Caso ping < 80, recebe  a cor verde verde
                            IconPingGame.Foreground = r.RoundtripTime > 80 ? Global.HexToColorBrushConverter("#ffb229") : Global.HexToColorBrushConverter("#4ad295");
                            _pingTabela = r.RoundtripTime;
                            GameMs.Text = $"{r.RoundtripTime} ms";
                            break;
                    }
                }
                //  Caso a resposta não for nula, nem de sucesso.
                //  Passamos o erro para a string respectiva de cada servidor
                else
                {
                    switch (ip)
                    {
                        //  Resposta Google
                        case "8.8.8.8":
                            respostaStatusGoogle.Sucesso = false;
                            respostaStatusGoogle.TempoDeResposta = r.RoundtripTime;
                            IconPingGoogle.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            GoogleMs.Text = r.Status.ToString();
                            break;
                        //  Resposta OpenDns
                        case "208.67.222.222":
                            respostaStatusOpenDns.Sucesso = false;
                            respostaStatusOpenDns.TempoDeResposta = r.RoundtripTime;
                            IconPingOpenDns.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            OpenDnsMs.Text = r.Status.ToString();
                            break;
                        //  Resposta CloudFlare
                        case "1.1.1.1":
                            respostaStatusCloudFlare.Sucesso = false;
                            respostaStatusCloudFlare.TempoDeResposta = r.RoundtripTime;
                            IconPingCloudflare.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            CloudflareMs.Text = r.Status.ToString();
                            break;
                        //  Resposta Riot
                        case "104.160.152.3":
                            respostaStatusRiot.Sucesso = false;
                            respostaStatusRiot.TempoDeResposta = r.RoundtripTime;
                            _pingTabela = 1000;
                            IconPingGame.Foreground = Global.HexToColorBrushConverter("#ff0000");
                            GameMs.Text = r.Status.ToString();
                            break;
                    }
                }
            }


            //  Verificamos o resultados 
            //  REDE ESTÁ OFF
            //  Para determinarmos isso, precisamos ver se todos servidores retornaram seu sucesso como falso
            if (respostaStatusGoogle.Sucesso == false && respostaStatusOpenDns.Sucesso == false && respostaStatusCloudFlare.Sucesso == false && respostaStatusRiot.Sucesso == false)
            {
                if (_status)
                {
                    // Constata queda
                    Logger.Escrever(Logger.LogType.Queda);
                    Global.Notificar("Sua rede caiu", "#ff0000");

                    HorarioDaQueda.Text = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                    _nQuedas += 1;
                    _status = false;
                    ContadorDeQuedas.Text = _nQuedas.ToString();
                    _watchQueda.Start();
                }
            }

            // REDE ESTÁ ON
            //  Para determinarmos isso, precisamos ver se qualquer servidor retornou seu sucesso como verdadeiro
            if (respostaStatusGoogle.Sucesso || respostaStatusOpenDns.Sucesso || respostaStatusCloudFlare.Sucesso || respostaStatusRiot.Sucesso)
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

            if (respostaStatusRiot.Sucesso)
            {
                _pingTabela = respostaStatusRiot.TempoDeResposta;
            }
            else
            {
                _pingTabela = 1000;
            }

            
            // Ping Jogo/Todos servidores

            //  Se a checkbox está ativa, então devemos considerar
            //  somente o ping do servidor da riot (que é o de jogos).
            if (CheckBoxPing.IsChecked.HasValue)
            {
                if (respostaStatusRiot.TempoDeResposta > 80)
                {
                    if (_status)
                    {
                        PingAlto.Text = $"{DateTime.Now.ToString(CultureInfo.CurrentCulture)} | {respostaStatusRiot.TempoDeResposta} ms";
                        Logger.Escrever(Logger.LogType.Latencia);
                        Global.Notificar("Ping alto servidor de jogo", "#ff0000");
                    }
                }
            }

            //  Caso ela esteja desativada, então devemos considerar
            //  o ping de todos os servidores
            else
            {
                if (respostaStatusGoogle.TempoDeResposta > 80 && respostaStatusOpenDns.TempoDeResposta > 80 && respostaStatusCloudFlare.TempoDeResposta > 80 && respostaStatusRiot.TempoDeResposta > 80)
                {
                    if (_status)
                    {
                        PingAlto.Text = $"{DateTime.Now.ToString(CultureInfo.CurrentCulture)} | {respostaStatusRiot.TempoDeResposta} ms";
                        Logger.Escrever(Logger.LogType.Latencia);
                        Global.Notificar("Ping alto servidor de jogo", "#ff0000");
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

            public long TempoDeResposta { get => _tempoDeResposta ?? 1000;
                set => _tempoDeResposta = value;
            }
            public bool Sucesso { get => _sucesso ?? false;
                set => _sucesso = value;
            }

        }

        // Botão minimizar
        private void ButtonMinimizar_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // Botão maximizar
        private void ButtonMaximizar_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        // Botão fechar
        //private void ButtonFechar_OnClick(object sender, RoutedEventArgs e)
        //{
        //    Application.Current.Shutdown();
        //}

        // Evento para mover a janela
        private void Rectangle_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Logger._pasta);
        }
        
        //  Adiciona ao startup do windows
        private void CheckBoxStartup_OnChecked(object sender, RoutedEventArgs e)
        {
            if (inicializando) return;
            var registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            registryKey?.SetValue("NetMonitor", Assembly.GetEntryAssembly()?.Location ?? throw new InvalidOperationException());
            registryKey?.Close();
        }

        //  Remove do startup do windows
        private void CheckBoxStartup_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (inicializando) return;
            var registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            registryKey?.DeleteValue("NetMonitor",false);
            registryKey?.Close();
        }

        //  Quando a janela estiver prestes a fechar ele salva as configurações
        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            Logger.Escrever(Logger.LogType.Final, TempoMonitorando.Text, ContadorDeQuedas.Text);
            NotifyIcon.Visible = false;
            NotifyIcon.Dispose();
        }
    }
}
