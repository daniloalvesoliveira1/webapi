using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApi.Models;

namespace WebApi.Controllers;

//https://localhost:7276/api/Status
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private string url = "";
    private readonly ILogger<StatusController> _logger;
    private readonly IConfiguration _config;
    public StatusController(ILogger<StatusController> logger,IConfiguration config)
    {
        _logger = logger;
        _config = config;
        url = _config.GetSection("AppSettings")["MssqlConnString"];
    }

    [HttpGet(Name = "GetStatus")]
    public List<Status> Get()
    {
        _logger.LogInformation("Executando GetStatus");

        List<Status> lista = new List<Status>();

        var query = "SELECT  id_status, [desc_status] FROM [TesteDB].[dbo].[Status]";

        using (SqlConnection connection = new SqlConnection(url))
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                _logger.LogInformation("Abrindo Conexão");
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    _logger.LogInformation("Retornando dados");
                    while (reader.Read())
                        lista.Add(new Status()
                        {
                            idStatus = reader.GetInt16(0),
                            descricaoStatus = reader.GetString(1).Trim()
                        });
                }
                connection.Close();
                connection.Dispose();
            }
        }
        _logger.LogInformation("Finalizando");
        return lista;
    }

}
