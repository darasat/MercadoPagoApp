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
                    return NotFound($"No se encontró el customer_id para el email: {email}");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al obtener el customer_id: {ex.Message}");
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromForm] decimal amount, [FromForm] string description, [FromForm] string cardNumber, [FromForm] int expirationMonth, [FromForm] int expirationYear, [FromForm] string cardholderName, [FromForm] string securityCode, [FromForm] string email)
        {
            try
            {
                // Generar el cardToken
                var cardToken = await _mercadoPagoService.GenerateCardTokenAsync(cardNumber, expirationMonth, expirationYear, cardholderName, securityCode);

                // Obtener el customerId del email proporcionado
                var customerId = await _mercadoPagoService.GetCustomerIdByEmailAsync(email);

                if (customerId == null)
                {
                    // Si no se encuentra el customerId, crear un nuevo cliente en MercadoPago
                    customerId = await _mercadoPagoService.CreateCustomerAsync(email);
                }

                // Asociar la tarjeta al cliente (opcional, dependiendo de la lógica de tu aplicación)
                // var cardId = await _mercadoPagoService.CreateCardAsync(customerId, cardToken);

                // Crear el pago utilizando los datos del formulario y el cardToken generado
                var payment = await _mercadoPagoService.CreatePaymentAsync(amount, description, customerId, cardToken, securityCode, email);

                return Ok(payment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al procesar el pago: {ex.Message}");
            }
        }
    }
}
