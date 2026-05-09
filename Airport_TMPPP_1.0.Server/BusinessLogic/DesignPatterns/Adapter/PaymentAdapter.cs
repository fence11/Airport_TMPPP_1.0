namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Adapter
{
    // one interface for any payment processor we want to use in the airport system
    public interface IPaymentProcessor
    {
        PaymentResult Charge(decimal amount, string currency, string description);
        bool Refund(string transactionId, decimal amount);
        string GatewayName { get; }
    }

    /// Returned by all <see cref="IPaymentProcessor"/> call
    public sealed record PaymentResult(
        bool   Success,
        string TransactionId,
        string Message);

    public sealed class PayPalSdk
    {
        public PayPalResponse MakePayment(
            double   usdAmount,
            string   payerEmail,
            string   note)
        {
            return new PayPalResponse(
                Status      : "COMPLETED",
                PayPalTxId  : $"PP-{Guid.NewGuid():N}",
                Detail      : $"PayPal charged {usdAmount:F2} USD – {note}");
        }

        public bool IssueRefund(string payPalTxId, double amount)
        {
            return true;
        }
    }

    public sealed record PayPalResponse(string Status, string PayPalTxId, string Detail);
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

    public sealed class PayPalAdapter : IPaymentProcessor
    {
        private readonly PayPalSdk _payPal;
        private const string SenderEmail = "airport-system@example.com";

        public PayPalAdapter(PayPalSdk payPal) =>
            _payPal = payPal ?? throw new ArgumentNullException(nameof(payPal));

        public string GatewayName => "PayPal";

        public PaymentResult Charge(decimal amount, string currency, string description)
        {
            double usdAmount = (double)amount;
            var response = _payPal.MakePayment(usdAmount, SenderEmail, description);

            bool success = response.Status == "COMPLETED";
            return new PaymentResult(success, response.PayPalTxId, response.Detail);
        }

        public bool Refund(string transactionId, decimal amount) =>
            _payPal.IssueRefund(transactionId, (double)amount);
    }

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

    public sealed class GooglePayAdapter : IPaymentProcessor
    {
        private readonly GooglePayApi _googlePay;

        public GooglePayAdapter(GooglePayApi googlePay) =>
            _googlePay = googlePay ?? throw new ArgumentNullException(nameof(googlePay));

        public string GatewayName => "Google Pay";

        public PaymentResult Charge(decimal amount, string currency, string description)
        {
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

            return result;
        }

        public bool RequestRefund(string transactionId, decimal amount)
        {
            bool ok = _processor.Refund(transactionId, amount);
            return ok;
        }
    }
}
