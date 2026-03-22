namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Adapter
{
    // ─────────────────────────────────────────────────────────────────────────
    // TARGET INTERFACE
    // The unified payment interface that the rest of the airport system uses.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The unified payment interface that every part of the airport system
    /// uses — regardless of which gateway is under the hood.
    /// </summary>
    public interface IPaymentProcessor
    {
        /// <summary>Charge the given amount in the specified currency.</summary>
        /// <returns>A transaction receipt.</returns>
        PaymentResult Charge(decimal amount, string currency, string description);

        /// <summary>Refund a previously completed transaction.</summary>
        bool Refund(string transactionId, decimal amount);

        /// <summary>Human-readable gateway name (useful for logging / UI).</summary>
        string GatewayName { get; }
    }

    /// <summary>Returned by every <see cref="IPaymentProcessor"/> call.</summary>
    public sealed record PaymentResult(
        bool   Success,
        string TransactionId,
        string Message);

    // ─────────────────────────────────────────────────────────────────────────
    // ADAPTEES  –  third-party SDKs with their own, incompatible APIs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Simulated PayPal SDK (incompatible API).</summary>
    public sealed class PayPalSdk
    {
        public PayPalResponse MakePayment(
            double   usdAmount,
            string   payerEmail,
            string   note)
        {
            // Simulate success for demo purposes.
            return new PayPalResponse(
                Status      : "COMPLETED",
                PayPalTxId  : $"PP-{Guid.NewGuid():N}",
                Detail      : $"PayPal charged {usdAmount:F2} USD – {note}");
        }

        public bool IssueRefund(string payPalTxId, double amount)
        {
            // Simulate refund.
            return true;
        }
    }

    public sealed record PayPalResponse(string Status, string PayPalTxId, string Detail);


    /// <summary>Simulated Stripe SDK (incompatible API).</summary>
    public sealed class StripeSdk
    {
        public StripeChargeResult CreateCharge(
            long     amountCents,
            string   currency,
            string   statementDescriptor)
        {
            return new StripeChargeResult(
                Id     : $"ch_{Guid.NewGuid():N}",
                Paid   : true,
                Amount : amountCents,
                Status : "succeeded");
        }

        public StripeRefundResult CreateRefund(string chargeId, long amountCents)
        {
            return new StripeRefundResult(
                Id     : $"re_{Guid.NewGuid():N}",
                Status : "succeeded");
        }
    }

    public sealed record StripeChargeResult(string Id, bool Paid, long Amount, string Status);
    public sealed record StripeRefundResult(string Id, string Status);


    /// <summary>Simulated Google Pay API (incompatible API).</summary>
    public sealed class GooglePayApi
    {
        public GooglePayResponse ProcessTransaction(
            string jsonPaymentToken,
            decimal totalPrice,
            string currencyCode)
        {
            return new GooglePayResponse(
                TransactionState : "SUCCESS",
                TransactionId    : $"GP-{Guid.NewGuid():N}");
        }

        public bool ReverseTransaction(string transactionId)
        {
            return true;
        }
    }

    public sealed record GooglePayResponse(string TransactionState, string TransactionId);

    // ─────────────────────────────────────────────────────────────────────────
    // ADAPTERS  –  wrap each SDK so it looks like IPaymentProcessor
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adapts <see cref="PayPalSdk"/> to <see cref="IPaymentProcessor"/>.
    /// Converts decimal amount/currency to USD double, maps results to
    /// the shared <see cref="PaymentResult"/> record.
    /// </summary>
    public sealed class PayPalAdapter : IPaymentProcessor
    {
        private readonly PayPalSdk _payPal;
        private const string SenderEmail = "airport-system@example.com";

        public PayPalAdapter(PayPalSdk payPal) =>
            _payPal = payPal ?? throw new ArgumentNullException(nameof(payPal));

        public string GatewayName => "PayPal";

        public PaymentResult Charge(decimal amount, string currency, string description)
        {
            // PayPal SDK only accepts USD doubles.
            double usdAmount = (double)amount;
            var response = _payPal.MakePayment(usdAmount, SenderEmail, description);

            bool success = response.Status == "COMPLETED";
            return new PaymentResult(success, response.PayPalTxId, response.Detail);
        }

        public bool Refund(string transactionId, decimal amount) =>
            _payPal.IssueRefund(transactionId, (double)amount);
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adapts <see cref="StripeSdk"/> to <see cref="IPaymentProcessor"/>.
    /// Converts decimal to cents (long) for Stripe.
    /// </summary>
    public sealed class StripeAdapter : IPaymentProcessor
    {
        private readonly StripeSdk _stripe;

        public StripeAdapter(StripeSdk stripe) =>
            _stripe = stripe ?? throw new ArgumentNullException(nameof(stripe));

        public string GatewayName => "Stripe";

        public PaymentResult Charge(decimal amount, string currency, string description)
        {
            long cents = (long)(amount * 100);
            var result = _stripe.CreateCharge(cents, currency.ToLower(), description);

            return new PaymentResult(
                result.Paid,
                result.Id,
                $"Stripe: {result.Status} – {result.Amount / 100.0:F2} {currency}");
        }

        public bool Refund(string transactionId, decimal amount)
        {
            long cents = (long)(amount * 100);
            var result = _stripe.CreateRefund(transactionId, cents);
            return result.Status == "succeeded";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adapts <see cref="GooglePayApi"/> to <see cref="IPaymentProcessor"/>.
    /// Constructs a minimal JSON payment token expected by the Google Pay API.
    /// </summary>
    public sealed class GooglePayAdapter : IPaymentProcessor
    {
        private readonly GooglePayApi _googlePay;

        public GooglePayAdapter(GooglePayApi googlePay) =>
            _googlePay = googlePay ?? throw new ArgumentNullException(nameof(googlePay));

        public string GatewayName => "Google Pay";

        public PaymentResult Charge(decimal amount, string currency, string description)
        {
            // Build a minimal JSON token as required by the Google Pay API.
            string token = $"{{\"description\":\"{description}\"}}";
            var response = _googlePay.ProcessTransaction(token, amount, currency);

            bool success = response.TransactionState == "SUCCESS";
            return new PaymentResult(
                success,
                response.TransactionId,
                $"Google Pay: {response.TransactionState} – {amount:F2} {currency}");
        }

        public bool Refund(string transactionId, decimal amount) =>
            _googlePay.ReverseTransaction(transactionId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PAYMENT SERVICE  –  client code that uses only IPaymentProcessor
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// High-level airport payment service.  It only ever talks to
    /// <see cref="IPaymentProcessor"/>; it has no knowledge of the underlying
    /// SDKs – that is the whole point of the Adapter pattern.
    /// </summary>
    public sealed class AirportPaymentService
    {
        private readonly IPaymentProcessor _processor;

        public AirportPaymentService(IPaymentProcessor processor) =>
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));

        public PaymentResult ProcessTicketPurchase(
            decimal amount,
            string  currency,
            string  flightNumber)
        {
            string description = $"Ticket purchase – flight {flightNumber}";
            var result = _processor.Charge(amount, currency, description);

            Console.WriteLine(
                $"[{_processor.GatewayName}] Ticket payment " +
                $"{(result.Success ? "succeeded" : "FAILED")}: {result.Message}");

            return result;
        }

        public bool RequestRefund(string transactionId, decimal amount)
        {
            bool ok = _processor.Refund(transactionId, amount);
            Console.WriteLine(
                $"[{_processor.GatewayName}] Refund {transactionId}: " +
                $"{(ok ? "approved" : "rejected")}");
            return ok;
        }
    }
}
