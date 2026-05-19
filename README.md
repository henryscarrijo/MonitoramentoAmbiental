[README.md](https://github.com/user-attachments/files/27979937/README.md)
# Plataforma de Monitoramento Ambiental — Rio Tietê

Sistema de comunicação em rede para monitoramento ambiental desenvolvido em C# com Sockets de Berkeley, Oracle Database XE e dashboard em HTML.

---

## Pré-requisitos

Antes de rodar o projeto, instale os seguintes programas:

### 1. .NET 10 SDK
- Acesse: https://dotnet.microsoft.com/download
- Baixe o **.NET 10 SDK** para Windows
- Instale normalmente (next, next, finish)
- Para verificar: abra o cmd e digite `dotnet --version`

### 2. Oracle Database 21c Express Edition
- Acesse: https://www.oracle.com/database/technologies/xe-downloads.html
- Baixe o **Oracle Database 21c Express Edition for Windows (64-bit)**
- Durante a instalação, defina uma senha para o usuário `system`
- **Anote essa senha** — você vai precisar dela!

---

## Configuração do Banco de Dados

Após instalar o Oracle XE, abra o **cmd** e siga os passos:

**1. Abre o sqlplus:**
```
sqlplus /nolog
```

**2. Conecta no banco (substitua SUASENHA pela senha que você definiu):**
```
conn system/"SUASENHA"@127.0.0.1:1521/xepdb1
```

**3. Executa o script de configuração:**
```
@setup_banco.sql
```

Isso vai criar todas as tabelas e inserir os dados de exemplo automaticamente.

---

## Configurar a senha no servidor C#

Abra o arquivo `Servidor/Program.cs` e localize a linha:

```csharp
string connStr = "User Id=system;Password=Look@32175173;Data Source=127.0.0.1:1521/xepdb1";
```

Substitua `Look@32175173` pela senha que você definiu na instalação do Oracle XE.

---

## Como rodar o sistema

### Terminal 1 — Servidor
```bash
cd Servidor
dotnet run
```
Aguarde aparecer:
```
TCP escutando na porta 5000...
HTTP escutando na porta 5001...
```

### Terminal 2 — Cliente TCP (opcional)
```bash
cd Cliente
dotnet run
```

### Dashboard e Formulário
- Abra o arquivo `formulario_inspetor.html` no navegador
- Abra o arquivo `dashboard.html` no navegador

---

## Como usar

1. Com o servidor rodando, abra o **formulário** no navegador
2. Selecione o inspetor, município, parâmetro, valor e nível de risco
3. Clique em **Enviar Medição**
4. Abra o **dashboard** para ver os dados em tempo real
5. Para verificar os dados no banco, abra o cmd e rode:

```sql
SELECT * FROM MEDICOES;
```

---

## Estrutura do Projeto

```
MonitoramentoAmbiental/
├── Servidor/
│   └── Program.cs          — Servidor TCP + HTTP + Oracle
├── Cliente/
│   └── Program.cs          — Cliente TCP com menu
├── formulario_inspetor.html — Interface web do inspetor
├── dashboard.html           — Dashboard em tempo real
└── setup_banco.sql          — Script de criação do banco
```

---

## Tecnologias Utilizadas

| Tecnologia | Finalidade |
|---|---|
| C# / .NET 10 | Servidor TCP e cliente |
| Sockets de Berkeley | Comunicação TCP/IP |
| Oracle Database 21c XE | Banco de dados local |
| HTML / JavaScript | Formulário e dashboard |

---

## Portas utilizadas

| Porta | Serviço |
|---|---|
| 5000 | Servidor TCP (Sockets de Berkeley) |
| 5001 | API HTTP REST (dashboard) |
| 1521 | Oracle Database XE |
