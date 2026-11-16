namespace SSKT_Events.Services;

public record PayMongoCheckoutResult(string CheckoutSessionId, string CheckoutUrl);

public class PayMongoService
{
    public Task<PayMongoCheckoutResult> CreateCheckoutSessionAsync(
        int paymentId,
        string eventName,
        decimal amountPhp,
        bool isDownPayment,
        string successUrl,
        string cancelUrl)
    {
        string sessionId = "cs_test_" + Guid.NewGuid().ToString("N")[..24];
        string checkoutUrl = $"/Events/MockCheckout?paymentId={paymentId}&sessionId={sessionId}";

        return Task.FromResult(new PayMongoCheckoutResult(sessionId, checkoutUrl));
    }

    public Task<bool> IsCheckoutSessionPaidAsync(string checkoutSessionId)
    {
        return Task.FromResult(checkoutSessionId.StartsWith("cs_test_"));
    }
}