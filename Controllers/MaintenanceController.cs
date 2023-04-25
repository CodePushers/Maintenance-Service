using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using RabbitMQ.Client;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Authorization;

namespace maintenanceService.Controllers;



[ApiController]
[Route("[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly ILogger<MaintenanceController> _logger;
    private readonly string _filePath;
    private readonly string _hostName;

    public MaintenanceController(ILogger<MaintenanceController> logger, IConfiguration config)
    {
        _logger = logger;
        // Henter miljø variabel "FilePath" og "HostnameRabbit" fra docker-compose
        _filePath = config["FilePath"] ?? "/srv";
        //_logger.LogInformation("FilePath er sat til: [$_filePath]"); virker måske
        _hostName = config["HostnameRabbit"];

        _logger.LogInformation($"Filepath: {_filePath}");
        _logger.LogInformation($"Connection: {_hostName}");

        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"Maintenance responding from {_ipaddr}");
    }

    // Opretter en PlanDTO ud fra BookingDTO
    [Authorize]
    [HttpPost("opretAnmodning")]
    public IActionResult OpretAnmodning([FromBody] Anmodning anmodning)
    {
        _logger.LogInformation($"Modtaget anmodningDTO:\n\tAnmodningID: {anmodning.AnmodningID}\n\tKøretøjID: {anmodning.KøretøjID}\n\tBeskrivelse: {anmodning.Beskrivelse}\n\tOpgavetype: {anmodning.OpgaveType}\n\tIndsender: {anmodning.Indsender}");

        AnmodningDTO anmodningDTO = new AnmodningDTO
        {
            AnmodningID = anmodning.AnmodningID,
            KøretøjID = anmodning.KøretøjID,
            Beskrivelse = anmodning.Beskrivelse,
            OpgaveType = anmodning.OpgaveType,
            Indsender = anmodning.Indsender
        };

        try
        {
            //Opretter forbindelse til RabbitMQ
            var factory = new ConnectionFactory
            {
                HostName = _hostName
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: "FleetService", type: ExchangeType.Topic);

            // Serialiseres til JSON
            string message = JsonSerializer.Serialize(anmodningDTO);

            _logger.LogInformation($"JsonSerialized message: \n\t{message}");
            // Konverteres til byte-array
            var body = Encoding.UTF8.GetBytes(message);

            if (anmodningDTO.OpgaveType == "Service")
            {
                // Sendes til Service-køen
                channel.BasicPublish(exchange: "FleetService",
                                     routingKey: "ServiceDTO",
                                     basicProperties: null,
                                     body: body);

                _logger.LogInformation($"ServiceDTO oprettet og sendt");

            }
            else if (anmodningDTO.OpgaveType == "Reparation")
            {
                // Sendes til Reparation-køen
                channel.BasicPublish(exchange: "FleetService",
                                     routingKey: "ReparationDTO",
                                     basicProperties: null,
                                     body: body);

                _logger.LogInformation($"ReparationDTO oprettet og sendt");

            }
            else
            {
                _logger.LogError($"Anmodning ikke sendt! Opgavetype: {anmodningDTO.OpgaveType}");
            }

        }

        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500);
        }
        return Ok(anmodningDTO);
    }

    // Henter CSV-fil
    [Authorize]
    [HttpGet("modtagRep")]
    public async Task<ActionResult> ModtagReparationPlan()
    {
        try
        {
            //Læser indholdet af CSV-fil fra filsti (_filePath)
            var bytes = await System.IO.File.ReadAllBytesAsync(Path.Combine(_filePath, "reparationPlan.csv"));

            _logger.LogInformation("reparationPlan.csv fil modtaget");

            // Returnere CSV-filen med indholdet
            return File(bytes, "text/csv", Path.GetFileName(Path.Combine(_filePath, "reparationPlan.csv")));

        }

        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500);
        }

    }

    // Henter CSV-fil
    [Authorize]
    [HttpGet("modtagService")]
    public async Task<ActionResult> ModtagServicePlan()
    {
        try
        {
            //Læser indholdet af CSV-fil fra filsti (_filePath)
            var bytes = await System.IO.File.ReadAllBytesAsync(Path.Combine(_filePath, "servicePlan.csv"));

            _logger.LogInformation("servicePlan.csv fil modtaget");

            // Returnere CSV-filen med indholdet
            return File(bytes, "text/csv", Path.GetFileName(Path.Combine(_filePath, "servicePlan.csv")));

        }

        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500);
        }

    }

    [Authorize]
    [HttpGet("version")]
    public IEnumerable<string> Get()
    {
        var properties = new List<string>();
        var assembly = typeof(Program).Assembly;
        foreach (var attribute in assembly.GetCustomAttributesData())
        {
            properties.Add($"{attribute.AttributeType.Name} - {attribute.ToString()}");
            _logger.LogInformation("Version blevet kaldt");
        }
        return properties;

    }

}

