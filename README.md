# MercadoPago .NET Core Integration

A comprehensive guide to integrate MercadoPago payment gateway with .NET Core Web API.

## 📋 Prerequisites

- Visual Studio 2019 or later
- .NET Core SDK
- MercadoPago developer account

## 🚀 Getting Started

### 1. Create a New Project

1. Open Visual Studio and create a new ASP.NET Core Web API project
2. Name your project `MercadoPagoApp`
3. Select the appropriate .NET framework version
4. Click Create

### 2. Install Required Packages

Install the MercadoPago SDK via NuGet Package Manager:

```bash
dotnet add package MercadoPago
```

Or through Visual Studio:
- Go to Tools → NuGet Package Manager → Manage NuGet Packages for Solution
- Search for "MercadoPago sdk" and install it

## ⚙️ Configuration

### 1. App Settings Configuration

Add your MercadoPago credentials to `appsettings.json`:

```json
{
  "MercadoPagoConfig": {
    "AccessToken": "your_access_token_here",
    "PublicKey": "your_public_key_here"
  }
}
```

> **Note:** Get your credentials from [MercadoPago Developers](https://www.mercadopago.com.co/developers/es) by registering your application.

### 2. Project Structure

Create the following folder structure:
```
MercadoPagoApp/
├── Controllers/
│   └── MercadoPagoController.cs
├── Services/
│   └── MercadoPagoService.cs
└── Configuration/
```

## 💻 Implementation

### Service Implementation

Create `Services/MercadoPagoService.cs`:

```csharp
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MercadoPagoApp.Services
{
    public interface IMercadoPagoService
    {
        Task<Payment> CreatePaymentAsync(decimal amount, string description, string customerId, string cardToken, string securityCode, string email);
        Task<string> CreateCustomerAsync(string email);
        Task<string> CreateCardAsync(string customerId, string cardToken);
        Task<string> GetCustomerIdByEmailAsync(string email);
        Task<string> GenerateCardTokenAsync(string cardNumber, int expirationMonth, int expirationYear, string cardholderName, string securityCode);
    }

    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public MercadoPagoService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            MercadoPagoConfig.AccessToken = _configuration["MercadoPagoConfig:AccessToken"];
        }

        public async Task<string> GetCustomerIdByEmailAsync(string email)
        {
            try
            {
                var accessToken = _configuration["MercadoPagoConfig:AccessToken"];
                var url = $"https://api.mercadopago.com/v1/customers/search?access_token={accessToken}&email={email}";
                
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var customers = JObject.Parse(responseBody)["results"] as JArray;
                    
                    if (customers != null && customers.Any())
                    {
                        var customer = customers.First();
                        return customer["id"].ToString();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching customer in MercadoPago by email: {ex.Message}");
                throw;
            }
        }

        public async Task<string> CreateCustomerAsync(string email)
        {
            try
            {
                var url = "https://api.mercadopago.com/v1/customers";
                var requestBody = new { email = email };
                
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var customer = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    return customer["id"].ToString();
                }
                throw new Exception($"Error creating customer: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating customer in MercadoPago: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GenerateCardTokenAsync(string cardNumber, int expirationMonth, int expirationYear, string cardholderName, string securityCode)
        {
            try
            {
                var url = "https://api.mercadopago.com/v1/card_tokens";
                var accessToken = _configuration["MercadoPagoConfig:AccessToken"];
                
                var requestData = new
                {
                    card_number = cardNumber,
                    expiration_month = expirationMonth,
                    expiration_year = expirationYear,
                    cardholder = new { name = cardholderName },
                    security_code = securityCode
                };

                var jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseBody);
                    return result.id;
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error generating cardToken: {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating cardToken in MercadoPago: {ex.Message}");
                throw;
            }
        }

        public async Task<string> CreateCardAsync(string customerId, string cardToken)
        {
            try
            {
                var url = $"https://api.mercadopago.com/v1/customers/{customerId}/cards";
                var requestBody = new { token = cardToken };
                
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var card = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    return card["id"].ToString();
                }
                throw new Exception($"Error associating card: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error associating card in MercadoPago: {ex.Message}");
                throw;
            }
        }

        public async Task<Payment> CreatePaymentAsync(decimal amount, string description, string customerId, string cardToken, string securityCode, string email)
        {
            try
            {
                var paymentRequest = new PaymentCreateRequest
                {
                    TransactionAmount = amount,
                    Token = cardToken,
                    Description = description,
                    Installments = 1,
                    PaymentMethodId = "visa", // Adjust according to payment method
                    Payer = new PaymentPayerRequest
                    {
                        Email = email,
                    }
                };

                var client = new PaymentClient();
                Payment payment = await client.CreateAsync(paymentRequest);
                return payment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating payment in MercadoPago: {ex.Message}");
                throw;
            }
        }
    }
}
```

### Controller Implementation

Create `Controllers/MercadoPagoController.cs`:

```csharp
using MercadoPagoApp.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MercadoPagoApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MercadoPagoController : ControllerBase
    {
        private readonly IMercadoPagoService _mercadoPagoService;

        public MercadoPagoController(IMercadoPagoService mercadoPagoService)
        {
            _mercadoPagoService = mercadoPagoService ?? throw new ArgumentNullException(nameof(mercadoPagoService));
        }

        [HttpGet("customerFind")]
        public async Task<IActionResult> GetCustomerId([FromQuery] string email)
        {
            try
            {
                var customerId = await _mercadoPagoService.GetCustomerIdByEmailAsync(email);
                if (customerId != null)
                {
                    return Ok(customerId);
                }
                else
                {
                    return NotFound($"Customer ID not found for email: {email}");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting customer ID: {ex.Message}");
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment(
            [FromForm] decimal amount, 
            [FromForm] string description, 
            [FromForm] string cardNumber, 
            [FromForm] int expirationMonth, 
            [FromForm] int expirationYear, 
            [FromForm] string cardholderName, 
            [FromForm] string securityCode, 
            [FromForm] string email)
        {
            try
            {
                // Generate cardToken
                var cardToken = await _mercadoPagoService.GenerateCardTokenAsync(cardNumber, expirationMonth, expirationYear, cardholderName, securityCode);
                
                // Get customerId from provided email
                var customerId = await _mercadoPagoService.GetCustomerIdByEmailAsync(email);
                if (customerId == null)
                {
                    // If customerId not found, create a new customer in MercadoPago
                    customerId = await _mercadoPagoService.CreateCustomerAsync(email);
                }

                // Create payment using form data and generated cardToken
                var payment = await _mercadoPagoService.CreatePaymentAsync(amount, description, customerId, cardToken, securityCode, email);
                return Ok(payment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing payment: {ex.Message}");
            }
        }
    }
}
```

### Program.cs Configuration

Update your `Program.cs`:

```csharp
using MercadoPago.Client.CardToken;
using MercadoPago.Client.Customer;
using MercadoPago.Client.Payment;
using MercadoPagoApp.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register MercadoPago services
builder.Services.AddScoped<CardTokenClient>();
builder.Services.AddScoped<CustomerClient>();
builder.Services.AddScoped<PaymentClient>();
builder.Services.AddScoped<IMercadoPagoService, MercadoPagoService>();

// Configure HttpClient for MercadoPagoService
builder.Services.AddHttpClient<IMercadoPagoService, MercadoPagoService>((serviceProvider, httpClient) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var accessToken = configuration["MercadoPagoConfig:AccessToken"];
    httpClient.BaseAddress = new Uri("https://api.mercadopago.com/v1/");
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    httpClient.DefaultRequestHeaders.Add("User-Agent", "MercadoPagoApp");
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MercadoPago API", Version = "v1" });
});

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MercadoPago API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## 🧪 Testing

### API Endpoints

The API provides two main endpoints:

1. **GET** `/api/MercadoPago/customerFind?email={email}`
   - Find existing customers by email

2. **POST** `/api/MercadoPago/create`
   - Create a new payment

### Test Payment Data

For testing, use MercadoPago's official test cards. Required parameters for payment creation:

- `amount`: Payment amount (decimal)
- `description`: Payment description
- `cardNumber`: Credit card number
- `expirationMonth`: Card expiration month (int)
- `expirationYear`: Card expiration year (int)
- `cardholderName`: Cardholder name
- `securityCode`: CVV code
- `email`: Customer email

### Example Test Request

```json
{
  "amount": 100.50,
  "description": "Test payment",
  "cardNumber": "4035874000006632",
  "expirationMonth": 11,
  "expirationYear": 2025,
  "cardholderName": "Test User",
  "securityCode": "123",
  "email": "test@example.com"
}
```

## 📚 Additional Resources

- [MercadoPago Official Documentation](https://www.mercadopago.com.co/developers/es)
- [Test Cards Reference](https://www.mercadopago.com.co/developers/es/docs/checkout-pro/additional-content/test-cards)

## 🛠️ Usage with Client Applications

After implementing this API, you can make HTTP calls from various client applications:

- Postman or Insomnia for API testing
- React/Angular web applications
- Mobile apps (Ionic, Flutter, etc.)
- Desktop applications

## 📈 Production Considerations

Before deploying to production:

1. Replace test credentials with production credentials
2. Implement proper error handling and logging
3. Add input validation and sanitization
4. Consider implementing rate limiting
5. Add proper authentication and authorization
6. Use HTTPS in production environment

## 🤝 Contributing

Feel free to submit issues and pull requests to improve this integration guide.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Note:** Remember to keep your MercadoPago credentials secure and never commit them to version control. Use environment variables or secure configuration management in production.
