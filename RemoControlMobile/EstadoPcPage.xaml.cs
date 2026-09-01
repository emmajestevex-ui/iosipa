using System.Net.Http.Json;
using System.Text;

namespace RemoControlMobile;

public partial class EstadoPcPage : ContentPage
{
    public EstadoPcPage()
    {
        InitializeComponent();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarEstado();
    }


    private async Task CargarEstado()
    {
        try
        {
            lblEstado.Text =
                "● Consultando...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(
                    10);

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    AppConfig.Servidor +
                    "/systemstatus");

            if (!respuesta.IsSuccessStatusCode)
            {
                MostrarSinConexion();

                return;
            }

            EstadoSistemaRespuesta? info =
                await respuesta.Content
                    .ReadFromJsonAsync
                    <EstadoSistemaRespuesta>();

            if (
                info == null ||
                !info.ok)
            {
                MostrarSinConexion();

                return;
            }


            // =====================================================
            // ESTADO
            // =====================================================

            lblEstado.Text =
                "● En línea";

            lblEstado.TextColor =
                Colors.LimeGreen;


            // =====================================================
            // PC
            // =====================================================

            lblPc.Text =
                "PC: " +
                (
                    string.IsNullOrWhiteSpace(
                        info.pc)
                        ? "--"
                        : info.pc
                );

            lblUsuario.Text =
                "Usuario: " +
                (
                    string.IsNullOrWhiteSpace(
                        info.user)
                        ? "--"
                        : info.user
                );

            lblWindows.Text =
                "Windows: " +
                (
                    string.IsNullOrWhiteSpace(
                        info.windows)
                        ? "--"
                        : info.windows
                );


            // =====================================================
            // CPU
            // =====================================================

            lblCpu.Text =
                info.cpu.ToString(
                    "0") +
                " %";


            // =====================================================
            // RAM
            // =====================================================

            lblRam.Text =
                info.ramUsed.ToString(
                    "0.0") +
                " / " +
                info.ramTotal.ToString(
                    "0.0") +
                " GB";


            // =====================================================
            // BATERÍA
            // =====================================================

            if (info.battery < 0)
            {
                lblBateria.Text =
                    "Sin batería";

                lblEstadoBateria.Text =
                    "Equipo de escritorio o batería no detectada";
            }
            else
            {
                lblBateria.Text =
                    info.battery +
                    " %";

                if (info.charging)
                {
                    lblEstadoBateria.Text =
                        "Estado: Cargando / conectado";
                }
                else
                {
                    lblEstadoBateria.Text =
                        "Estado: Usando batería";
                }

                if (
                    info.battery <= 20 &&
                    !info.charging)
                {
                    lblBateria.TextColor =
                        Colors.OrangeRed;
                }
                else
                {
                    lblBateria.TextColor =
                        Colors.White;
                }
            }


            // =====================================================
            // RED
            // =====================================================

            lblIp.Text =
                "IP: " +
                (
                    string.IsNullOrWhiteSpace(
                        info.ip)
                        ? "--"
                        : info.ip
                );

            lblConexion.Text =
                "Conexión: " +
                ObtenerTipoConexion();


            // =====================================================
            // TIEMPO ENCENDIDA
            // =====================================================

            lblTiempo.Text =
                string.IsNullOrWhiteSpace(
                    info.uptime)
                    ? "--"
                    : info.uptime;


            // =====================================================
            // DISCOS
            // =====================================================

            MostrarDiscos(
                info.drives);
        }
        catch
        {
            MostrarSinConexion();
        }
    }


    private void MostrarDiscos(
        List<DiscoSistema>? discos)
    {
        if (
            discos == null ||
            discos.Count == 0)
        {
            lblDiscos.Text =
                "No hay información de discos.";

            return;
        }

        StringBuilder texto =
            new StringBuilder();

        foreach (
            DiscoSistema disco
            in discos)
        {
            double usado =
                disco.total -
                disco.free;

            double porcentaje =
                0;

            if (disco.total > 0)
            {
                porcentaje =
                    (
                        usado /
                        disco.total
                    ) *
                    100;
            }

            texto.AppendLine(
                "💽 " +
                (
                    disco.name ??
                    "Disco"
                ));

            texto.AppendLine(
                "Libre: " +
                disco.free.ToString(
                    "0.0") +
                " GB de " +
                disco.total.ToString(
                    "0.0") +
                " GB");

            texto.AppendLine(
                "Uso: " +
                porcentaje.ToString(
                    "0") +
                "%");

            texto.AppendLine();
        }

        lblDiscos.Text =
            texto.ToString()
                .Trim();
    }


    private string ObtenerTipoConexion()
    {
        try
        {
            string servidor =
                AppConfig.Servidor ??
                "";

            if (
                servidor.Contains(
                    "192.168.") ||
                servidor.Contains(
                    "10.") ||
                servidor.Contains(
                    "172."))
            {
                return
                    "Wi-Fi local";
            }

            if (
                servidor.Contains(
                    "100."))
            {
                return
                    "Tailscale";
            }

            return
                "Servidor personalizado";
        }
        catch
        {
            return
                "Desconocida";
        }
    }


    private void MostrarSinConexion()
    {
        lblEstado.Text =
            "● Sin conexión";

        lblEstado.TextColor =
            Colors.Red;

        lblPc.Text =
            "PC: --";

        lblUsuario.Text =
            "Usuario: --";

        lblWindows.Text =
            "Windows: --";

        lblCpu.Text =
            "-- %";

        lblRam.Text =
            "--";

        lblBateria.Text =
            "--";

        lblEstadoBateria.Text =
            "Estado: --";

        lblIp.Text =
            "IP: --";

        lblConexion.Text =
            "Conexión: --";

        lblTiempo.Text =
            "--";

        lblDiscos.Text =
            "No disponible";
    }


    private async void BtnActualizar_Clicked(
        object sender,
        EventArgs e)
    {
        await CargarEstado();
    }


    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}


// ============================================================
// JSON DEL SERVIDOR
// ============================================================

public class EstadoSistemaRespuesta
{
    public bool ok
    {
        get;
        set;
    }

    public string? pc
    {
        get;
        set;
    }

    public string? user
    {
        get;
        set;
    }

    public string? windows
    {
        get;
        set;
    }

    public string? ip
    {
        get;
        set;
    }

    public double cpu
    {
        get;
        set;
    }

    public double ramTotal
    {
        get;
        set;
    }

    public double ramUsed
    {
        get;
        set;
    }

    public int battery
    {
        get;
        set;
    }

    public bool charging
    {
        get;
        set;
    }

    public string? uptime
    {
        get;
        set;
    }

    public List<DiscoSistema>? drives
    {
        get;
        set;
    }

    public string? error
    {
        get;
        set;
    }
}


public class DiscoSistema
{
    public string? name
    {
        get;
        set;
    }

    public double total
    {
        get;
        set;
    }

    public double free
    {
        get;
        set;
    }
}