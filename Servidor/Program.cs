using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;

Console.WriteLine("=== Servidor de Monitoramento Ambiental ===");
Console.WriteLine("TCP escutando na porta 5000...");
Console.WriteLine("HTTP escutando na porta 5001...");

string connStr = "User Id=system;Password=Look@32175173;Data Source=127.0.0.1:1521/xepdb1";

Thread httpThread = new Thread(() => IniciarHttp(connStr));
httpThread.IsBackground = true;
httpThread.Start();

var servidor = new TcpListener(IPAddress.Any, 5000);
servidor.Start();

while (true)
{
    TcpClient cliente = servidor.AcceptTcpClient();
    string ip = ((IPEndPoint)cliente.Client.RemoteEndPoint!).Address.ToString();
    Console.WriteLine($"\n[TCP] Inspetor conectado! IP: {ip}");

    Thread thread = new Thread(() => TratarTcp(cliente, ip, connStr));
    thread.IsBackground = true;
    thread.Start();
}

void TratarTcp(TcpClient cliente, string ip, string connStr)
{
    var reader = new StreamReader(cliente.GetStream());
    var writer = new StreamWriter(cliente.GetStream()) { AutoFlush = true };

    writer.WriteLine("Conectado ao servidor de monitoramento. Envie seus dados:");

    string? linha;
    while ((linha = reader.ReadLine()) != null)
    {
        Console.WriteLine($"[TCP] {ip}: {linha}");
        try
        {
            var dados = JsonSerializer.Deserialize<Dictionary<string, object>>(linha);
            if (dados != null)
            {
                SalvarNoBanco(dados, connStr);
                writer.WriteLine("OK - Dado salvo no banco!");
                Console.WriteLine("[DB] Salvo com sucesso!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO] {ex.Message}");
            writer.WriteLine("OK - Dado recebido!");
        }
    }
    Console.WriteLine($"[TCP] Inspetor {ip} desconectou.");
}

void IniciarHttp(string connStr)
{
    var http = new HttpListener();
    http.Prefixes.Add("http://localhost:5001/");
    http.Start();
    Console.WriteLine("[HTTP] Servidor HTTP pronto!");

    while (true)
    {
        var ctx = http.GetContext();
        Thread t = new Thread(() => TratarHttp(ctx, connStr));
        t.IsBackground = true;
        t.Start();
    }
}

void TratarHttp(HttpListenerContext ctx, string connStr)
{
    ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
    ctx.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
    ctx.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

    if (ctx.Request.HttpMethod == "OPTIONS")
    {
        ctx.Response.StatusCode = 200;
        ctx.Response.Close();
        return;
    }

    try
    {
        string path = ctx.Request.Url?.AbsolutePath ?? "/";

        // GET /dados — retorna medições em JSON
        if (ctx.Request.HttpMethod == "GET" && path == "/dados")
        {
            var medicoes = BuscarDados(connStr);
            string json = JsonSerializer.Serialize(medicoes);
            byte[] resp = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            ctx.Response.OutputStream.Write(resp);
            ctx.Response.Close();
            return;
        }

        // GET /resumo — retorna cards de resumo
        if (ctx.Request.HttpMethod == "GET" && path == "/resumo")
        {
            var resumo = BuscarResumo(connStr);
            string json = JsonSerializer.Serialize(resumo);
            byte[] resp = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            ctx.Response.OutputStream.Write(resp);
            ctx.Response.Close();
            return;
        }

        // POST /medicao — salva medição
        if (ctx.Request.HttpMethod == "POST" && path == "/medicao")
        {
            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            string body = reader.ReadToEnd();
            Console.WriteLine($"[HTTP] Recebido: {body}");

            var dados = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
            if (dados != null)
            {
                SalvarNoBanco(dados, connStr);
                Console.WriteLine("[DB] Salvo via HTTP com sucesso!");
                byte[] resp = Encoding.UTF8.GetBytes("Dado salvo no banco com sucesso!");
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "text/plain; charset=utf-8";
                ctx.Response.OutputStream.Write(resp);
            }
            ctx.Response.Close();
            return;
        }

        ctx.Response.StatusCode = 404;
        ctx.Response.Close();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO HTTP] {ex.Message}");
        byte[] resp = Encoding.UTF8.GetBytes($"Erro: {ex.Message}");
        ctx.Response.StatusCode = 500;
        ctx.Response.OutputStream.Write(resp);
        ctx.Response.Close();
    }
}

List<object> BuscarDados(string connStr)
{
    var lista = new List<object>();
    using var conn = new OracleConnection(connStr);
    conn.Open();

    var cmd = new OracleCommand(@"
        SELECT i.NOME, ind.MUNICIPIO, m.PARAMETRO, m.VALOR, m.UNIDADE, m.NIVEL_RISCO, r.DT_ENVIO
        FROM MEDICOES m
        , RELATORIOS r
        , INSPETORES i
        , INDUSTRIAS ind
        WHERE m.ID_RELATORIO  = r.ID_RELATORIO
        AND   r.ID_INSPETOR   = i.ID_INSPETOR
        AND   r.ID_INDUSTRIA  = ind.ID_INDUSTRIA
        ORDER BY r.DT_ENVIO DESC", conn);

    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        lista.Add(new {
            inspetor  = reader.GetString(0),
            municipio = reader.GetString(1),
            parametro = reader.GetString(2),
            valor     = reader.GetDouble(3),
            unidade   = reader.IsDBNull(4) ? "" : reader.GetString(4),
            risco     = reader.IsDBNull(5) ? "NORMAL" : reader.GetString(5),
            data      = reader.GetDateTime(6).ToString("dd/MM/yyyy HH:mm")
        });
    }
    return lista;
}

object BuscarResumo(string connStr)
{
    using var conn = new OracleConnection(connStr);
    conn.Open();

    int totalMedicoes = Convert.ToInt32(new OracleCommand("SELECT COUNT(*) FROM MEDICOES", conn).ExecuteScalar());
    int totalInspetores = Convert.ToInt32(new OracleCommand("SELECT COUNT(*) FROM INSPETORES WHERE STATUS = 'ONLINE'", conn).ExecuteScalar());
    int totalRelatorios = Convert.ToInt32(new OracleCommand("SELECT COUNT(*) FROM RELATORIOS", conn).ExecuteScalar());
    int totalCriticos = Convert.ToInt32(new OracleCommand("SELECT COUNT(*) FROM MEDICOES WHERE NIVEL_RISCO = 'CRITICO'", conn).ExecuteScalar());

    return new {
        medicoes    = totalMedicoes,
        inspetores  = totalInspetores,
        relatorios  = totalRelatorios,
        criticos    = totalCriticos
    };
}

void SalvarNoBanco(Dictionary<string, object> dados, string connStr)
{
    using var conn = new OracleConnection(connStr);
    conn.Open();

    string nomeInspetor = dados["nome_inspetor"].ToString()!.Trim();
    var cmdInspetor = new OracleCommand(
        "SELECT ID_INSPETOR FROM INSPETORES WHERE UPPER(NOME) = UPPER(:p1) AND ROWNUM = 1", conn);
    cmdInspetor.Parameters.Add(":p1", nomeInspetor);
    var idInspetor = cmdInspetor.ExecuteScalar();
    if (idInspetor == null) throw new Exception("Inspetor não encontrado!");

    string municipio = dados["municipio"].ToString()!.Trim();
    var cmdIndustria = new OracleCommand(
        "SELECT ID_INDUSTRIA FROM INDUSTRIAS WHERE UPPER(MUNICIPIO) = UPPER(:p1) AND ROWNUM = 1", conn);
    cmdIndustria.Parameters.Add(":p1", municipio);
    var idIndustria = cmdIndustria.ExecuteScalar();
    if (idIndustria == null) throw new Exception("Indústria não encontrada!");

    var cmdRel = new OracleCommand(
        "INSERT INTO RELATORIOS (ID_INSPETOR, ID_INDUSTRIA, TIPO, DESCRICAO, STATUS) " +
        "VALUES (:p1, :p2, :p3, :p4, 'RECEBIDO') RETURNING ID_RELATORIO INTO :p5", conn);
    cmdRel.Parameters.Add(":p1", idInspetor);
    cmdRel.Parameters.Add(":p2", idIndustria);
    cmdRel.Parameters.Add(":p3", dados["tipo"].ToString());
    cmdRel.Parameters.Add(":p4", dados["descricao"].ToString());
    var paramId = new OracleParameter(":p5", OracleDbType.Decimal, System.Data.ParameterDirection.Output);
    cmdRel.Parameters.Add(paramId);
    cmdRel.ExecuteNonQuery();

    decimal idRelatorio = ((Oracle.ManagedDataAccess.Types.OracleDecimal)paramId.Value).Value;

    var cmdMed = new OracleCommand(
        "INSERT INTO MEDICOES (ID_RELATORIO, PARAMETRO, VALOR, UNIDADE, NIVEL_RISCO) " +
        "VALUES (:p1, :p2, :p3, :p4, :p5)", conn);
    cmdMed.Parameters.Add(":p1", idRelatorio);
    cmdMed.Parameters.Add(":p2", dados["parametro"].ToString());
    cmdMed.Parameters.Add(":p3", double.Parse(dados["valor"].ToString()!));
    cmdMed.Parameters.Add(":p4", dados["unidade"].ToString());
    cmdMed.Parameters.Add(":p5", dados["nivel_risco"].ToString());
    cmdMed.ExecuteNonQuery();

    var cmdCommit = new OracleCommand("COMMIT", conn);
    cmdCommit.ExecuteNonQuery();
}