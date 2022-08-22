using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OpenTelemetry;
using WebApi.Models;
using Confluent.Kafka;

namespace WebApi.Controllers;

//https://localhost:7276/api/Solicitacao

[ApiController]
[Route("api/[controller]")]
public class SolicitacaoController : ControllerBase
{    
    private readonly ILogger<SolicitacaoController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly IConfiguration _config;

    private readonly ActivitySource source = new ActivitySource(nameof(SolicitacaoController));

    private string url = "";
    public SolicitacaoController(ILogger<SolicitacaoController> logger, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _config = config;
        url = _config.GetSection("AppSettings")["MssqlConnString"];
    }

    /// <summary>
    /// Lista as solicitações
    /// </summary>
    /// <param name="id_solicitacao">Consultar por id (opcional)</param>
    /// <param name="id_status">Consultar por id do status (Opicional)</param>
    /// <returns></returns>
    [HttpGet(Name = "GetSolicitacao")]
    public List<Solicitacao> Get(Int64? id_solicitacao, Int16? id_status)
    {
        _logger.LogError("Executando GetSolicitacao");
        List<Solicitacao> lista = new List<Solicitacao>();

        var query = "  select id_solicitacao, data_hora_solicitacao,  desc_status, desc_solicitacao, s1.id_status FROM [TesteDB].[dbo].[Solicitacao] as s1,   [TesteDB].[dbo].[Status] as s2        where s1.id_status = s2.id_status ";
        if (id_solicitacao != null)
            query += " AND s1.id_solicitacao = @id_solicitacao";
        if (id_status != null)
            query += " AND s1.id_status = @id_status";

        using (SqlConnection connection = new SqlConnection(url))
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                _logger.LogInformation("Abrindo Conexão");
                connection.Open();

                if (id_solicitacao != null)
                    command.Parameters.Add(new SqlParameter("@id_solicitacao", id_solicitacao));
                if (id_status != null)
                    command.Parameters.Add(new SqlParameter("@id_status", id_status));

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    _logger.LogInformation("Retornando dados");
                    while (reader.Read())
                        lista.Add(new Solicitacao()
                        {
                            idSolicitacao = reader.GetInt64(0),
                            dataSolicitacao = reader.GetDateTime(1),
                            descricaoSolicitacao = reader.GetString(3).Trim(),
                            status = new Status()
                            {
                                descricaoStatus = reader.GetString(2).Trim(),
                                idStatus = reader.GetInt16(4)
                            }
                        });
                }
                connection.Close();
                connection.Dispose();
            }
        }
        _logger.LogInformation("Finalizando");

        // A span
        using var activity = source.StartActivity("Chamando api ZOS Connect");
        activity?.SetTag("TagMANUAL2", "InfoManual2");

        // _logger.LogWarning("Acesso HTTP api zosconnect");
        // var client = _httpClientFactory.CreateClient();

        // client.BaseAddress = new Uri("https://apiibmdev.mercantil.com.br:9444/mb.api.fab.cadastromodelo/");
        // //HTTP GET
        // var responseTask = client.GetAsync("bip");
        // responseTask.Wait();

        // var result = responseTask.Result;
        // if (result.IsSuccessStatusCode)
        // {
        //     var readTask = result.Content.ReadAsStringAsync();
        //     readTask.Wait();
        // }
        // else
        // {
        //     ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
        // }
        // _logger.LogInformation("Finalizando");

        using var activity2 = source.StartActivity("@ Finalizando api ZOS Connect");
        activity2?.SetTag("TagMANUAL", "InfoManual");

        return lista;
    }
    [HttpPost(Name = "PostSolicitacao")]
    public void IncluirSolicitacao(Int16 id_status, String descricao_solicitacao)
    {
        _logger.LogInformation("Executando GetSolicitacao");

        List<Solicitacao> lista = new List<Solicitacao>();

        try
        {
            GravaKafka("Inclusão->{descricao_solicitacao" + descricao_solicitacao + "}");
        }
        catch { }

        var query = "  insert into [TesteDB].[dbo].[Solicitacao] (data_hora_solicitacao, id_status, desc_solicitacao)  values (getdate(),@id_status,@desc_solicitacao) ";
        using (SqlConnection connection = new SqlConnection(url))
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                _logger.LogInformation("Abrindo Conexão");
                connection.Open();
                command.Parameters.Add(new SqlParameter("@id_status", id_status));
                command.Parameters.Add(new SqlParameter("@desc_solicitacao", descricao_solicitacao));
                _logger.LogInformation("Inserindo Solicitacao");
                command.ExecuteNonQuery();
                connection.Close();
                connection.Dispose();
            }
        }
        _logger.LogInformation("Finalizando");
    }

    [HttpPut(Name = "PutSolicitacao")]
    public void AlterarStatusSolicitacao(Int64 id_solicitacao, Int16 id_status)
    {
        _logger.LogInformation("Executando PutSolicitacao");

        List<Solicitacao> lista = new List<Solicitacao>();

        var query = "  Update[TesteDB].[dbo].[Solicitacao] SET id_status = @id_status  where id_solicitacao = @id_solicitacao";

        using (SqlConnection connection = new SqlConnection(url))
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                _logger.LogInformation("Abrindo Conexão");
                connection.Open();
                command.Parameters.Add(new SqlParameter("@id_status", id_status));
                command.Parameters.Add(new SqlParameter("@id_solicitacao", id_solicitacao));
                _logger.LogInformation("Alterando status Solicitacao");
                command.ExecuteNonQuery();
                connection.Close();
                connection.Dispose();
            }
        }
        _logger.LogInformation("Finalizando");
    }

    [HttpDelete(Name = "DeleteSolicitacao")]
    public void ExcluirStatusSolicitacao(Int64 id_solicitacao)
    {
        _logger.LogInformation("Executando DeleteSolicitacao");

        try
        {
            GravaKafka("Exclusão->{id_solicitacao" + id_solicitacao + "}");
        }
        catch { }


        var query = "  Delete from [TesteDB].[dbo].[Solicitacao] WHERE id_solicitacao = @id_solicitacao";

        using (SqlConnection connection = new SqlConnection(url))
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                _logger.LogInformation("Abrindo Conexão");
                connection.Open();
                command.Parameters.Add(new SqlParameter("@id_solicitacao", id_solicitacao));
                _logger.LogInformation("Excluindo Solicitacao");
                command.ExecuteNonQuery();
                connection.Close();
                connection.Dispose();
            }
        }
        _logger.LogInformation("Finalizando");
    }

    private async void GravaKafka(string mensagem)
    {

        string bootstrapServers = "kafka:9092";
        string nomeTopic = "topico-danilo";

        using var activity = source.StartActivity("Produzindo Mensagem Kafka");
        activity?.SetTag("Tópico", nomeTopic);

        _logger.LogInformation($"Topic = {bootstrapServers}");
        _logger.LogInformation($"BootstrapServers = {bootstrapServers}");

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            using (var producer = new ProducerBuilder<Null, string>(config).Build())
            {

                var result = await producer.ProduceAsync(
                    nomeTopic,
                    new Message<Null, string>
                    { Value = mensagem });

                _logger.LogInformation(
                    $"Mensagem: {mensagem} | " +
                    $"Status: {result.Status.ToString()}");

            }

            _logger.LogInformation("Concluído o envio de mensagens");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exceção: {ex.GetType().FullName} | " +
                         $"Mensagem: {ex.Message}");
        }
    }
}
