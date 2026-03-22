namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Facade
{
    // ─────────────────────────────────────────────────────────────────────────
    // SUBSYSTEM CLASSES  –  complex internal services hidden by the Façade
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Searches available rooms in the hotel inventory.</summary>
    public sealed class RoomSearchService
    {
        private static readonly Dictionary<string, RoomInfo> _catalog = new()
        {
            ["STD"] = new("Standard",     2, 90m,  "Double bed, city view"),
            ["DLX"] = new("Deluxe",       2, 150m, "King bed, balcony, airport view"),
            ["STE"] = new("Suite",        4, 280m, "Two rooms, lounge, VIP amenities"),
            ["FAM"] = new("Family",       4, 180m, "Two double beds, kitchenette"),
        };

        public IEnumerable<RoomInfo> FindAvailableRooms(
            DateTime checkIn,
            DateTime checkOut,
            int      guestCount)
        {
            Console.WriteLine(
                $"  [RoomSearch] Searching rooms for {guestCount} guest(s) " +
                $"{checkIn:dd-MMM} → {checkOut:dd-MMM}");

            return _catalog.Values.Where(r => r.MaxGuests >= guestCount);
        }

        public RoomInfo? GetRoomInfo(string roomTypeCode) =>
            _catalog.TryGetValue(roomTypeCode.ToUpper(), out var info) ? info : null;
    }

    public sealed record RoomInfo(
        string  TypeName,
        int     MaxGuests,
        decimal PricePerNight,
        string  Description);

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Checks and manages room availability calendar.</summary>
    public sealed class AvailabilityService
    {
        // In a real system this would query a database.
        // Here we simulate: all rooms available unless they match a blocked date.
        private static readonly HashSet<string> _blocked = new()
        {
            "STE-2026-07-04", "STE-2026-07-05"   // suite blocked on Independence Day
        };

        public bool IsAvailable(string roomTypeCode, DateTime checkIn, DateTime checkOut)
        {
            Console.WriteLine(
                $"  [Availability] Checking {roomTypeCode} " +
                $"{checkIn:dd-MMM} – {checkOut:dd-MMM}");

            for (var d = checkIn; d < checkOut; d = d.AddDays(1))
            {
                string key = $"{roomTypeCode.ToUpper()}-{d:yyyy-MM-dd}";
                if (_blocked.Contains(key))
                {
                    Console.WriteLine($"  [Availability] ✗ {key} is blocked.");
                    return false;
                }
            }

            Console.WriteLine($"  [Availability] ✓ Room available.");
            return true;
        }

        public void BlockDates(string roomTypeCode, DateTime checkIn, DateTime checkOut)
        {
            for (var d = checkIn; d < checkOut; d = d.AddDays(1))
                _blocked.Add($"{roomTypeCode.ToUpper()}-{d:yyyy-MM-dd}");

            Console.WriteLine($"  [Availability] Dates blocked for {roomTypeCode}.");
        }

        public void ReleaseDates(string roomTypeCode, DateTime checkIn, DateTime checkOut)
        {
            for (var d = checkIn; d < checkOut; d = d.AddDays(1))
                _blocked.Remove($"{roomTypeCode.ToUpper()}-{d:yyyy-MM-dd}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Handles payment processing for reservations.</summary>
    public sealed class HotelPaymentService
    {
        public HotelPaymentResult Charge(
            string  guestName,
            decimal totalAmount,
            string  paymentMethod)
        {
            Console.WriteLine(
                $"  [Payment] Charging {totalAmount:F2} MDL " +
                $"via {paymentMethod} for {guestName}");

            // Simulate success.
            return new HotelPaymentResult(
                Success       : true,
                TransactionId : $"HTX-{Guid.NewGuid():N}[..8]",
                Message       : $"Payment of {totalAmount:F2} MDL confirmed.");
        }

        public bool Refund(string transactionId, decimal amount)
        {
            Console.WriteLine($"  [Payment] Refunding {amount:F2} MDL – txn {transactionId}");
            return true;
        }
    }

    public sealed record HotelPaymentResult(
        bool   Success,
        string TransactionId,
        string Message);

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Sends booking confirmation e-mails and SMS.</summary>
    public sealed class NotificationService
    {
        public void SendConfirmationEmail(string email, HotelBookingConfirmation booking)
        {
            Console.WriteLine(
                $"  [Email] Confirmation sent to {email}: " +
                $"Booking #{booking.BookingReference}");
        }

        public void SendConfirmationSms(string phone, string bookingRef)
        {
            Console.WriteLine(
                $"  [SMS] Confirmation sent to {phone}: Booking #{bookingRef}");
        }

        public void SendCancellationNotice(string email, string bookingRef)
        {
            Console.WriteLine(
                $"  [Email] Cancellation notice sent to {email}: " +
                $"Booking #{bookingRef} cancelled.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Manages the bookings ledger (CRUD on reservations).</summary>
    public sealed class BookingRepository
    {
        private readonly Dictionary<string, HotelBookingConfirmation> _store = new();

        public HotelBookingConfirmation Save(HotelBookingConfirmation booking)
        {
            _store[booking.BookingReference] = booking;
            Console.WriteLine($"  [Repository] Booking #{booking.BookingReference} saved.");
            return booking;
        }

        public HotelBookingConfirmation? Find(string reference) =>
            _store.TryGetValue(reference, out var b) ? b : null;

        public bool Delete(string reference)
        {
            bool removed = _store.Remove(reference);
            if (removed) Console.WriteLine($"  [Repository] Booking #{reference} deleted.");
            return removed;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SHARED DTOs
    // ─────────────────────────────────────────────────────────────────────────

    public sealed record HotelBookingConfirmation(
        string   BookingReference,
        string   GuestName,
        string   RoomType,
        DateTime CheckIn,
        DateTime CheckOut,
        decimal  TotalPrice,
        string   TransactionId);

    // ─────────────────────────────────────────────────────────────────────────
    // FAÇADE  –  single, simple entry-point for the entire subsystem
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <para>
    ///   <b>Façade</b>: exposes three simple, high-level operations
    ///   (<see cref="BookRoom"/>, <see cref="CancelBooking"/>,
    ///   <see cref="SearchRooms"/>) while hiding the interactions between
    ///   five internal subsystem classes.
    /// </para>
    /// <para>
    ///   Client code only needs to know about this class; it never touches
    ///   <see cref="RoomSearchService"/>, <see cref="AvailabilityService"/>,
    ///   <see cref="HotelPaymentService"/>, <see cref="NotificationService"/>,
    ///   or <see cref="BookingRepository"/> directly.
    /// </para>
    /// </summary>
    public sealed class HotelReservationFacade
    {
        // ── subsystem dependencies ──────────────────────────────────────────
        private readonly RoomSearchService   _roomSearch;
        private readonly AvailabilityService _availability;
        private readonly HotelPaymentService _payment;
        private readonly NotificationService _notification;
        private readonly BookingRepository   _repository;

        public HotelReservationFacade(
            RoomSearchService?   roomSearch    = null,
            AvailabilityService? availability  = null,
            HotelPaymentService? payment       = null,
            NotificationService? notification  = null,
            BookingRepository?   repository    = null)
        {
            _roomSearch    = roomSearch    ?? new RoomSearchService();
            _availability  = availability  ?? new AvailabilityService();
            _payment       = payment       ?? new HotelPaymentService();
            _notification  = notification  ?? new NotificationService();
            _repository    = repository    ?? new BookingRepository();
        }

        // ── public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Books a room in ONE call.  Internally this coordinates:
        /// room lookup → availability check → price calculation →
        /// payment → persistence → notifications.
        /// </summary>
        public HotelBookingResult BookRoom(HotelBookingRequest request)
        {
            Console.WriteLine($"\n[Façade] BookRoom requested by {request.GuestName}");

            // 1. Fetch room details.
            var room = _roomSearch.GetRoomInfo(request.RoomTypeCode);
            if (room is null)
                return HotelBookingResult.Failure($"Room type '{request.RoomTypeCode}' not found.");

            // 2. Check capacity.
            if (request.GuestCount > room.MaxGuests)
                return HotelBookingResult.Failure(
                    $"Room '{room.TypeName}' accommodates max {room.MaxGuests} guests.");

            // 3. Check availability.
            if (!_availability.IsAvailable(request.RoomTypeCode, request.CheckIn, request.CheckOut))
                return HotelBookingResult.Failure(
                    $"Room '{room.TypeName}' is not available for the requested period.");

            // 4. Calculate total price.
            int     nights     = (int)(request.CheckOut - request.CheckIn).TotalDays;
            decimal totalPrice = room.PricePerNight * nights;

            // 5. Process payment.
            var payment = _payment.Charge(request.GuestName, totalPrice, request.PaymentMethod);
            if (!payment.Success)
                return HotelBookingResult.Failure($"Payment failed: {payment.Message}");

            // 6. Block the dates.
            _availability.BlockDates(request.RoomTypeCode, request.CheckIn, request.CheckOut);

            // 7. Persist booking.
            string reference = $"BK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}[..4]";
            var confirmation = _repository.Save(new HotelBookingConfirmation(
                BookingReference : reference,
                GuestName        : request.GuestName,
                RoomType         : room.TypeName,
                CheckIn          : request.CheckIn,
                CheckOut         : request.CheckOut,
                TotalPrice       : totalPrice,
                TransactionId    : payment.TransactionId));

            // 8. Send notifications.
            _notification.SendConfirmationEmail(request.GuestEmail, confirmation);
            if (!string.IsNullOrWhiteSpace(request.GuestPhone))
                _notification.SendConfirmationSms(request.GuestPhone, reference);

            Console.WriteLine($"[Façade] Booking completed: #{reference}");
            return HotelBookingResult.Ok(confirmation);
        }

        /// <summary>
        /// Cancels an existing booking, refunds payment and sends notices.
        /// </summary>
        public bool CancelBooking(string bookingReference)
        {
            Console.WriteLine($"\n[Façade] CancelBooking #{bookingReference}");

            var booking = _repository.Find(bookingReference);
            if (booking is null)
            {
                Console.WriteLine($"[Façade] Booking #{bookingReference} not found.");
                return false;
            }

            // Refund.
            _payment.Refund(booking.TransactionId, booking.TotalPrice);

            // Release calendar.
            string roomCode = booking.RoomType.Substring(0, 3).ToUpper();
            _availability.ReleaseDates(roomCode, booking.CheckIn, booking.CheckOut);

            // Remove record.
            _repository.Delete(bookingReference);

            // Notify.
            _notification.SendCancellationNotice(
                $"{booking.GuestName.Replace(" ", ".").ToLower()}@example.com",
                bookingReference);

            Console.WriteLine($"[Façade] Cancellation complete for #{bookingReference}");
            return true;
        }

        /// <summary>
        /// Simple room search – delegates to the search subsystem.
        /// </summary>
        public IEnumerable<RoomInfo> SearchRooms(
            DateTime checkIn,
            DateTime checkOut,
            int      guestCount = 1)
        {
            Console.WriteLine($"\n[Façade] SearchRooms – {guestCount} guest(s)");
            return _roomSearch.FindAvailableRooms(checkIn, checkOut, guestCount);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REQUEST / RESULT DTOs used by the Façade
    // ─────────────────────────────────────────────────────────────────────────

    public sealed record HotelBookingRequest(
        string   GuestName,
        string   GuestEmail,
        string?  GuestPhone,
        string   RoomTypeCode,
        int      GuestCount,
        DateTime CheckIn,
        DateTime CheckOut,
        string   PaymentMethod = "Credit Card");

    public sealed class HotelBookingResult
    {
        public bool                     Success     { get; }
        public string?                  ErrorMessage { get; }
        public HotelBookingConfirmation? Confirmation { get; }

        private HotelBookingResult(
            bool success,
            string? error,
            HotelBookingConfirmation? confirmation)
        {
            Success      = success;
            ErrorMessage = error;
            Confirmation = confirmation;
        }

        public static HotelBookingResult Ok(HotelBookingConfirmation c)  =>
            new(true,  null,  c);

        public static HotelBookingResult Failure(string error) =>
            new(false, error, null);
    }
}
