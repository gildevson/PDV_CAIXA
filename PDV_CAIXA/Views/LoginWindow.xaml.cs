using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using PDV_CAIXA.Services;

namespace PDV_CAIXA.Views {
    public partial class LoginWindow : Window {
        private readonly AuthService _authService = new();

        private int      _tentativas   = 0;
        private DateTime _bloqueioAte  = DateTime.MinValue;
        private System.Windows.Threading.DispatcherTimer? _timerBloqueio;

        private const int MaxTentativas    = 3;
        private const int SegundosBloqueio = 30;

        public LoginWindow() {
            InitializeComponent();
            txtUsuario.Focus();
        }

        // ── Entrar com Enter no campo senha ──────────────────────────
        private void PwdSenha_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter)
                BtnLogin_Click(sender, e);
        }

        // ── Login ─────────────────────────────────────────────────────
        private async void BtnLogin_Click(object sender, RoutedEventArgs e) {
            // Verifica bloqueio
            if (DateTime.Now < _bloqueioAte) {
                var restam = (int)(_bloqueioAte - DateTime.Now).TotalSeconds;
                MostrarErro($"Muitas tentativas incorretas.", $"Aguarde {restam}s para tentar novamente.", "⏳");
                Shake();
                return;
            }

            LimparErro();
            btnLogin.IsEnabled = false;

            var nome  = txtUsuario.Text?.Trim() ?? string.Empty;
            var senha = pwdSenha.Password ?? string.Empty;

            // Campos vazios
            if (string.IsNullOrEmpty(nome) && string.IsNullOrEmpty(senha)) {
                MostrarErro("Preencha usuário e senha.", icone: "⚠");
                txtUsuario.Focus();
                btnLogin.IsEnabled = true;
                return;
            }
            if (string.IsNullOrEmpty(nome)) {
                MostrarErro("Informe o nome de usuário.", icone: "⚠");
                txtUsuario.Focus();
                btnLogin.IsEnabled = true;
                return;
            }
            if (string.IsNullOrEmpty(senha)) {
                MostrarErro("Informe a senha.", icone: "⚠");
                pwdSenha.Focus();
                btnLogin.IsEnabled = true;
                return;
            }

            try {
                var usuario = await _authService.ValidateCredentialsAsync(nome, senha);

                if (usuario != null) {
                    // Login OK — reseta tentativas
                    _tentativas = 0;
                    var main = new MainWindow(usuario);
                    main.Show();
                    Close();
                } else {
                    // Credenciais erradas
                    _tentativas++;
                    int restam = MaxTentativas - _tentativas;

                    if (_tentativas >= MaxTentativas) {
                        // Bloqueia
                        _bloqueioAte = DateTime.Now.AddSeconds(SegundosBloqueio);
                        _tentativas  = 0;
                        MostrarErro(
                            "Conta bloqueada temporariamente.",
                            $"Aguarde {SegundosBloqueio}s após muitas tentativas incorretas.",
                            "🔒");
                        IniciarContadorBloqueio();
                    } else {
                        MostrarErro(
                            "Usuário ou senha incorretos.",
                            restam == 1
                                ? $"⚠ Última tentativa antes do bloqueio!"
                                : $"{restam} tentativa(s) restante(s)",
                            "🔐");
                    }

                    pwdSenha.Clear();
                    pwdSenha.Focus();
                    Shake();
                }
            } catch (Exception ex) {
                MostrarErro("Erro ao conectar com o banco de dados.", ex.Message, "❌");
            } finally {
                btnLogin.IsEnabled = true;
            }
        }

        // ── Exibir / limpar erro ──────────────────────────────────────
        private void MostrarErro(string mensagem, string? detalhe = null, string icone = "🔐") {
            txtStatus.Text          = mensagem;
            txtErroIcone.Text       = icone;
            borderErro.Visibility   = Visibility.Visible;

            if (detalhe != null) {
                txtTentativas.Text       = detalhe;
                txtTentativas.Visibility = Visibility.Visible;
            } else {
                txtTentativas.Visibility = Visibility.Collapsed;
            }
        }

        private void LimparErro() {
            borderErro.Visibility    = Visibility.Collapsed;
            txtStatus.Text           = string.Empty;
            txtTentativas.Visibility = Visibility.Collapsed;
        }

        // ── Shake animation na tela ───────────────────────────────────
        private void Shake() {
            var transform = (System.Windows.Media.TranslateTransform)pwdSenha.RenderTransform;

            var anim = new DoubleAnimationUsingKeyFrames {
                Duration = TimeSpan.FromMilliseconds(400)
            };
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0,   KeyTime.FromTimeSpan(TimeSpan.Zero)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(-10, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(10,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(-8,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(8,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(-4,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(310))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(4,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(360))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0,   KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));

            transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim);
        }

        // ── Contador regressivo de bloqueio ───────────────────────────
        private void IniciarContadorBloqueio() {
            _timerBloqueio?.Stop();
            _timerBloqueio = new System.Windows.Threading.DispatcherTimer {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timerBloqueio.Tick += (_, _) => {
                if (DateTime.Now >= _bloqueioAte) {
                    _timerBloqueio.Stop();
                    LimparErro();
                    txtUsuario.Focus();
                    return;
                }

                var restam = (int)(_bloqueioAte - DateTime.Now).TotalSeconds;
                txtTentativas.Text = $"Aguarde {restam}s para tentar novamente.";
            };
            _timerBloqueio.Start();
        }
    }
}
