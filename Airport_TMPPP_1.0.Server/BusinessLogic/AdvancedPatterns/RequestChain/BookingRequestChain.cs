using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;
using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.ChainOfResponsibility
{
    // ── Request & Result ──────────────────────────────────────────────────────

    public sealed class BookingRequest
    {
        public string PassengerName  { get; init; } = string.Empty;
        public string PassengerEmail { get; init; } = string.Empty;
        public int    FlightId       { get; init; }
        public string FlightNumber   { get; init; } = string.Empty;
        public int    BaggageWeightKg{ get; init; }
        public bool   HasSpecialMeal { get; init; }
        public string PaymentMethod  { get; init; } = "CreditCard";
        public decimal TicketPrice   { get; init; }
    }

    public sealed class HandlerStep
    {
        public string HandlerName { get; init; } = string.Empty;
        public bool   Passed      { get; init; }
        public string Message     { get; init; } = string.Empty;
    }

    public sealed class BookingResult
    {
        public bool   Success      { get; init; }
        public string BookingRef   { get; init; } = string.Empty;
        public string FinalMessage { get; init; } = string.Empty;
        public IReadOnlyList<HandlerStep> ChainTrace { get; init; } = Array.Empty<HandlerStep>();
    }

    // ── Abstract handler ──────────────────────────────────────────────────────

    public abstract class BookingHandler
    {
        protected BookingHandler? _next;
        protected readonly List<HandlerStep> _trace;

        protected BookingHandler(List<HandlerStep> trace) => _trace = trace;

        public BookingHandler SetNext(BookingHandler next)
        {
            _next = next;
            return next;
        }

        public abstract Task<bool> HandleAsync(BookingRequest request, IUnitOfWork uow);

        protected async Task<bool> PassToNextAsync(BookingRequest request, IUnitOfWork uow)
        {
            if (_next is null) return true;
            return await _next.HandleAsync(request, uow);
        }
    }

    // ── Concrete handlers ─────────────────────────────────────────────────────

    /// Verifies the flight exists in the database.
    public sealed class FlightExistsHandler : BookingHandler
    {
        public FlightExistsHandler(List<HandlerStep> trace) : base(trace) { }

        public override async Task<bool> HandleAsync(BookingRequest request, IUnitOfWork uow)
        {
            var flight = await uow.Flights.GetByIdAsync(request.FlightId);
            bool ok = flight is not null &&
                      flight.FlightNumber.Equals(request.FlightNumber, StringComparison.OrdinalIgnoreCase);

            _trace.Add(new HandlerStep
            {
                HandlerName = "FlightExistsCheck",
                Passed      = ok,
                Message     = ok
                    ? $"Flight {request.FlightNumber} confirmed in database (ID {request.FlightId})."
                    : $"Flight {request.FlightNumber} / ID {request.FlightId} not found in database."
            });

            return ok && await PassToNextAsync(request, uow);
        }
    }

    /// Validates passenger record exists.
    public sealed class PassengerValidationHandler : BookingHandler
    {
        public PassengerValidationHandler(List<HandlerStep> trace) : base(trace) { }

        public override async Task<bool> HandleAsync(BookingRequest request, IUnitOfWork uow)
        {
            var passengers = await uow.Passengers.GetAllAsync();
            var passenger = passengers.FirstOrDefault(p =>
                p.Email.Equals(request.PassengerEmail, StringComparison.OrdinalIgnoreCase));

            bool ok = passenger is not null;
            _trace.Add(new HandlerStep
            {
                HandlerName = "PassengerValidation",
                Passed      = ok,
                Message     = ok
                    ? $"Passenger '{passenger!.FirstName} {passenger.LastName}' found (ID {passenger.Id})."
                    : $"No passenger record found for email '{request.PassengerEmail}'."
            });

            return ok && await PassToNextAsync(request, uow);
        }
    }

    /// Enforces baggage weight policy (max 32 kg).
    public sealed class BaggageCheckHandler : BookingHandler
    {
        private const int MaxWeightKg = 32;
        public BaggageCheckHandler(List<HandlerStep> trace) : base(trace) { }

        public override async Task<bool> HandleAsync(BookingRequest request, IUnitOfWork uow)
        {
            bool ok = request.BaggageWeightKg >= 0 && request.BaggageWeightKg <= MaxWeightKg;
            _trace.Add(new HandlerStep
            {
                HandlerName = "BaggageWeightCheck",
                Passed      = ok,
                Message     = ok
                    ? $"Baggage {request.BaggageWeightKg} kg is within the {MaxWeightKg} kg limit."
                    : $"Baggage {request.BaggageWeightKg} kg exceeds the {MaxWeightKg} kg limit. Excess: {request.BaggageWeightKg - MaxWeightKg} kg."
            });

            return ok && await PassToNextAsync(request, uow);
        }
    }

    /// Validates payment method is accepted.
    public sealed class PaymentValidationHandler : BookingHandler
    {
        private static readonly HashSet<string> _accepted =
            new(StringComparer.OrdinalIgnoreCase) { "CreditCard", "DebitCard", "BankTransfer", "PayPal" };

        public PaymentValidationHandler(List<HandlerStep> trace) : base(trace) { }

        public override async Task<bool> HandleAsync(BookingRequest request, IUnitOfWork uow)
        {
            bool ok = _accepted.Contains(request.PaymentMethod) && request.TicketPrice > 0;
            _trace.Add(new HandlerStep
            {
                HandlerName = "PaymentValidation",
                Passed      = ok,
                Message     = ok
                    ? $"Payment of {request.TicketPrice:F2} MDL via {request.PaymentMethod} approved."
                    : !_accepted.Contains(request.PaymentMethod)
                        ? $"Payment method '{request.PaymentMethod}' is not accepted."
                        : "Ticket price must be positive."
            });

            return ok && await PassToNextAsync(request, uow);
        }
    }

    /// Final handler – persists the booking log entry to the DB.
    public sealed class BookingConfirmationHandler : BookingHandler
    {
        public BookingConfirmationHandler(List<HandlerStep> trace) : base(trace) { }

        public override async Task<bool> HandleAsync(BookingRequest request, IUnitOfWork uow)
        {
            // Persist a log entry by updating the passenger's contact info with timestamp
            // (real project would have a Bookings table; we touch Passenger to prove DB write)
            var passengers = await uow.Passengers.GetAllAsync();
            var passenger = passengers.First(p =>
                p.Email.Equals(request.PassengerEmail, StringComparison.OrdinalIgnoreCase));

            passenger.UpdateContactInfo(passenger.PhoneNumber); // triggers UpdatedAt via MarkUpdated()
            await uow.Passengers.UpdateAsync(passenger);

            _trace.Add(new HandlerStep
            {
                HandlerName = "BookingConfirmation",
                Passed      = true,
                Message     = $"Booking confirmed. DB record updated for passenger ID {passenger.Id}. Ref: {BookingRefFor(request)}"
            });

            return true;
        }

        internal static string BookingRefFor(BookingRequest r) =>
            $"BKG-{r.FlightNumber}-{DateTime.UtcNow:yyyyMMddHHmm}";
    }

    // ── Pipeline builder ──────────────────────────────────────────────────────

    public static class BookingChainBuilder
    {
        public static (BookingHandler head, List<HandlerStep> trace) Build()
        {
            var trace = new List<HandlerStep>();

            var flightCheck  = new FlightExistsHandler(trace);
            var passengerVal = new PassengerValidationHandler(trace);
            var baggageCheck = new BaggageCheckHandler(trace);
            var paymentVal   = new PaymentValidationHandler(trace);
            var confirmation = new BookingConfirmationHandler(trace);

            flightCheck
                .SetNext(passengerVal)
                .SetNext(baggageCheck)
                .SetNext(paymentVal)
                .SetNext(confirmation);

            return (flightCheck, trace);
        }

        public static async Task<BookingResult> ProcessAsync(BookingRequest request, IUnitOfWork uow)
        {
            var (head, trace) = Build();
            bool success = await head.HandleAsync(request, uow);

            return new BookingResult
            {
                Success      = success,
                BookingRef   = success ? BookingConfirmationHandler.BookingRefFor(request) : string.Empty,
                FinalMessage = success
                    ? $"Booking complete for {request.PassengerName} on flight {request.FlightNumber}."
                    : $"Booking rejected at: {trace.LastOrDefault(s => !s.Passed)?.HandlerName ?? "unknown"}.",
                ChainTrace   = trace.AsReadOnly()
            };
        }
    }
}
