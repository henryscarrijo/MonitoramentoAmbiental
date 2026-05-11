using System.Net.Sockets;
using System.Text.Json;

Console.WriteLine("=== Cliente - Inspetor de Campo ===");

Console.Write("Seu nome: ");
string nome = Console.ReadLine() ?? "Inspetor";

Console.WriteLine("\nEscolha o município:");
Console.WriteLine("1 - Salesópolis");
Console.WriteLine("2 - Mogi das Cruzes");
Console.WriteLine("3 - Santo André");
Console.Write("Opção: ");
string opcao = Console.ReadLine() ?? "1";

string municipio = opcao switch {
    "1" => "Salesopolis",
    "2" => "Mogi das Cruzes",
    "3" => "Santo Andre",
    _   => "Salesopolis"
};

Console.WriteLine("\nEscolha o parâmetro:");
Console.WriteLine("1 - pH");
Console.WriteLine("2 - DBO");
Console.WriteLine("3 - Metais Pesados");
Console.Write("Opção: ");
string opcaoParam = Console.ReadLine() ?? "1";

string parametro = opcaoParam switch {
    "1" => "pH",
    "2" => "DBO",
    "3" => "Metais Pesados",
    _   => "pH"
};

Console.Write("Valor medido: ");
double valor = double.Parse(Console.ReadLine() ?? "0");

Console.WriteLine("\nNível de risco:");
Console.WriteLine("1 - NORMAL");
Console.WriteLine("2 - ATENCAO");
Console.WriteLine("3 - CRITICO");
Console.Write("Opção: ");
string opcaoRisco = Console.ReadLine() ?? "1";

string risco = opcaoRisco switch {
    "1" => "NORMAL",
    "2" => "ATENCAO",
    "3" => "CRITICO",
    _   => "NORMAL"
};

// Monta o JSON com os dados
var dados = new {
    nome_inspetor = nome,
    municipio     = municipio,
    tipo          = "ROTINA",
    descricao     = $"Medição de {parametro} em {municipio}",
    parametro     = parametro,
    valor         = valor,
    unidade       = parametro == "pH" ? "pH" : "mg/L",
    nivel_risco   = risco
};

string json = JsonSerializer.Serialize(dados);

// Conecta no servidor e envia
TcpClient cliente = new TcpClient("127.0.0.1", 5000);
var reader = new StreamReader(cliente.GetStream());
var writer = new StreamWriter(cliente.GetStream()) { AutoFlush = true };

Console.WriteLine("\n" + reader.ReadLine());

writer.WriteLine(json);
Console.WriteLine(reader.ReadLine());

cliente.Close();
Console.WriteLine("\nDados enviados com sucesso!");
Console.ReadKey();