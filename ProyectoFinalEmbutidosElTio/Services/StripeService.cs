using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Configuration;
using ProyectoFinalEmbutidosElTio.Models.ViewModels;

namespace ProyectoFinalEmbutidosElTio.Services
{
    public class StripeService
    {
        private readonly IConfiguration _configuration;

        public StripeService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public Session CreateCheckoutSession(CartViewModel cart, string domain)
        {
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + "/Carrito/StripeSuccess?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = domain + "/Carrito/Checkout",
                PaymentMethodTypes = new List<string> { "card" },
            };

            foreach (var item in cart.Items)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = item.Producto.Precio_final * 100, // Stripe uses cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Producto.Nombre,
                            Description = item.Producto.Descripcion ?? "Producto de Embutidos El Tío"
                        },
                    },
                    Quantity = item.Quantity,
                });
            }

            var service = new SessionService();
            Session session = service.Create(options);
            return session;
        }

        public Session GetSession(string sessionId)
        {
            var service = new SessionService();
            return service.Get(sessionId);
        }
    }
}
