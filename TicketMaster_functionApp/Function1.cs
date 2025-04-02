using System;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace TicketMaster_functionApp
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run([QueueTrigger("tickethub", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");

            string json = message.MessageText;
            //Deserizalize the message JSON into a Contact object

            //use options to make case insensitive
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };


            TicketForm? ticketForm = JsonSerializer.Deserialize<TicketForm>(json,options);

            if (ticketForm == null)
            {
                _logger.LogError("Failed to deserialize the message into a tickethub obj");
                return;
            }

            _logger.LogInformation($"Hello {ticketForm.Name}!");

            //add contact to database;

            //get connection string from app settings
            string? connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("SQL connection string is not set in the environment variables.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                var query = @"
                    INSERT INTO dbo.ConcertTicketInfo (
                        ConcertId, Email, Name, Phone, Quantity,
                        CreditCard, Expiration, SecurityCode,
                        Address, City, Province, PostalCode, Country
                    ) VALUES (
                        @ConcertId, @Email, @Name, @Phone, @Quantity,
                        @CreditCard, @Expiration, @SecurityCode,
                        @Address, @City, @Province, @PostalCode, @Country
                    )";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ConcertId", ticketForm.ConcertId);
                    cmd.Parameters.AddWithValue("@Email", ticketForm.Email);
                    cmd.Parameters.AddWithValue("@Name", ticketForm.Name);
                    cmd.Parameters.AddWithValue("@Phone", ticketForm.Phone);
                    cmd.Parameters.AddWithValue("@Quantity", ticketForm.Quantity);
                    cmd.Parameters.AddWithValue("@CreditCard", ticketForm.CreditCard);
                    cmd.Parameters.AddWithValue("@Expiration", ticketForm.Expiration);
                    cmd.Parameters.AddWithValue("@SecurityCode", ticketForm.SecurityCode);
                    cmd.Parameters.AddWithValue("@Address", ticketForm.Address);
                    cmd.Parameters.AddWithValue("@City", ticketForm.City);
                    cmd.Parameters.AddWithValue("@Province", ticketForm.Province);
                    cmd.Parameters.AddWithValue("@PostalCode", ticketForm.PostalCode);  
                    cmd.Parameters.AddWithValue("@Country", ticketForm.Country);

                    await cmd.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("ticketForm added to database");
            }


        }
    }
}
