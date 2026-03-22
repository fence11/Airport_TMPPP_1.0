import { useState } from "react";
import {
  adapterPay,
  getRestaurantMenu,
  filterMenu,
  searchRooms,
  bookRoom,
  cancelBooking,
  type Gateway,
  type AdapterPaymentResponse,
  type MenuItemDto,
  type RoomInfo,
  type BookingConfirmation,
} from "../../api/structuralPatternsApi";

// ─────────────────────────────────────────────────────────────────────────────
// Small shared helpers
// ─────────────────────────────────────────────────────────────────────────────

const Tag = ({ label, color }: { label: string; color: string }) => (
  <span
    style={{
      fontSize: "0.7rem",
      padding: "0.15rem 0.5rem",
      borderRadius: "999px",
      background: color,
      color: "#fff",
      marginLeft: "0.4rem",
      fontWeight: 600,
      letterSpacing: "0.05em",
      textTransform: "uppercase",
    }}
  >
    {label}
  </span>
);

const SectionCard = ({
  title,
  badge,
  badgeColor,
  description,
  children,
}: {
  title: string;
  badge: string;
  badgeColor: string;
  description: string;
  children: React.ReactNode;
}) => (
  <div
    style={{
      border: "1px solid #2a2a3a",
      borderRadius: "12px",
      padding: "1.5rem",
      marginBottom: "2rem",
      background: "#13131f",
    }}
  >
    <h3 style={{ margin: "0 0 0.25rem", display: "flex", alignItems: "center", gap: "0.5rem" }}>
      {title}
      <Tag label={badge} color={badgeColor} />
    </h3>
    <p style={{ color: "#888", fontSize: "0.85rem", marginBottom: "1.25rem" }}>{description}</p>
    {children}
  </div>
);

const Field = ({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) => (
  <div style={{ marginBottom: "0.75rem" }}>
    <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.85rem", color: "#aaa" }}>
      {label}
    </label>
    {children}
  </div>
);

const inputStyle: React.CSSProperties = {
  width: "100%",
  padding: "0.45rem 0.6rem",
  boxSizing: "border-box",
  background: "#1e1e2e",
  border: "1px solid #333",
  borderRadius: "6px",
  color: "#eee",
  fontSize: "0.9rem",
};

const btnStyle = (disabled: boolean, accent = "#5c6bc0"): React.CSSProperties => ({
  padding: "0.5rem 1.2rem",
  background: disabled ? "#2a2a3a" : accent,
  color: disabled ? "#555" : "#fff",
  border: "none",
  borderRadius: "6px",
  cursor: disabled ? "not-allowed" : "pointer",
  fontWeight: 600,
  fontSize: "0.9rem",
});

const ResultBox = ({ children }: { children: React.ReactNode }) => (
  <div
    style={{
      marginTop: "1rem",
      padding: "0.9rem 1rem",
      background: "#0e0e1a",
      border: "1px solid #2a2a3a",
      borderRadius: "8px",
      fontSize: "0.85rem",
    }}
  >
    {children}
  </div>
);

// ─────────────────────────────────────────────────────────────────────────────
// 1.  ADAPTER – Payment Gateway
// ─────────────────────────────────────────────────────────────────────────────

const AdapterSection = () => {
  const [gateway, setGateway] = useState<Gateway>("paypal");
  const [name, setName] = useState("Jane Doe");
  const [amount, setAmount] = useState("250");
  const [currency, setCurrency] = useState("MDL");
  const [flight, setFlight] = useState("RO123");
  const [result, setResult] = useState<AdapterPaymentResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const GATEWAYS: { value: Gateway; label: string; color: string }[] = [
    { value: "paypal",    label: "PayPal",     color: "#003087" },
    { value: "stripe",    label: "Stripe",     color: "#635bff" },
    { value: "googlepay", label: "Google Pay", color: "#4285f4" },
  ];

  const submit = async () => {
    setError(null);
    setResult(null);
    const amt = parseFloat(amount);
    if (!name.trim() || isNaN(amt) || amt <= 0) {
      setError("Please fill in all fields with valid values.");
      return;
    }
    setLoading(true);
    try {
      const res = await adapterPay({
        gateway,
        customerName: name.trim(),
        amount: amt,
        currency,
        flightNumber: flight,
      });
      setResult(res);
    } catch {
      setError("Payment request failed – check the console.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <SectionCard
      title="Payment Gateway"
      badge="Adapter"
      badgeColor="#e65100"
      description="The same AirportPaymentService processes every payment through IPaymentProcessor. Pick any gateway – the adapter translates the call to the incompatible SDK underneath."
    >
      {/* Gateway selector */}
      <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1rem", flexWrap: "wrap" }}>
        {GATEWAYS.map((g) => (
          <button
            key={g.value}
            onClick={() => setGateway(g.value)}
            style={{
              ...btnStyle(false, g.color),
              opacity: gateway === g.value ? 1 : 0.4,
              outline: gateway === g.value ? `2px solid ${g.color}` : "none",
              outlineOffset: "2px",
            }}
          >
            {g.label}
          </button>
        ))}
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))", gap: "0.5rem" }}>
        <Field label="Customer name">
          <input style={inputStyle} value={name} onChange={(e) => setName(e.target.value)} />
        </Field>
        <Field label="Amount">
          <input style={inputStyle} type="number" min={1} value={amount} onChange={(e) => setAmount(e.target.value)} />
        </Field>
        <Field label="Currency">
          <input style={inputStyle} value={currency} onChange={(e) => setCurrency(e.target.value)} />
        </Field>
        <Field label="Flight number">
          <input style={inputStyle} value={flight} onChange={(e) => setFlight(e.target.value)} />
        </Field>
      </div>

      <button onClick={submit} disabled={loading} style={{ ...btnStyle(loading), marginTop: "0.5rem" }}>
        {loading ? "Processing…" : `Pay with ${GATEWAYS.find((g) => g.value === gateway)?.label}`}
      </button>

      {error && <p style={{ color: "#ef5350", marginTop: "0.75rem" }}>{error}</p>}

      {result && (
        <ResultBox>
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", marginBottom: "0.5rem" }}>
            <span style={{ fontSize: "1.1rem" }}>{result.success ? "✅" : "❌"}</span>
            <strong>{result.gateway}</strong>
            <Tag label={result.success ? "SUCCESS" : "FAILED"} color={result.success ? "#2e7d32" : "#c62828"} />
          </div>
          <div style={{ color: "#aaa", lineHeight: 1.7 }}>
            <div><span style={{ color: "#666" }}>Transaction ID: </span>{result.transactionId}</div>
            <div><span style={{ color: "#666" }}>Message: </span>{result.message}</div>
          </div>
        </ResultBox>
      )}
    </SectionCard>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 2.  COMPOSITE – Restaurant Menu
// ─────────────────────────────────────────────────────────────────────────────

const MenuNode = ({ node, depth = 0 }: { node: MenuItemDto; depth?: number }) => {
  const [open, setOpen] = useState(depth < 2);
  const isLeaf = !node.children || node.children.length === 0;

  const catColors: Record<string, string> = {
    Food: "#1565c0",
    Drink: "#00695c",
    Dessert: "#6a1b9a",
  };

  if (isLeaf) {
    return (
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          padding: "0.35rem 0.6rem",
          borderRadius: "4px",
          background: "#0e0e1a",
          marginBottom: "3px",
        }}
      >
        <span style={{ color: "#ccc" }}>
          {node.name}
          {node.category && (
            <Tag label={node.category} color={catColors[node.category] ?? "#555"} />
          )}
          {node.description && (
            <span style={{ color: "#555", fontSize: "0.78rem", marginLeft: "0.4rem" }}>
              – {node.description}
            </span>
          )}
        </span>
        <span style={{ color: "#81c784", fontWeight: 600, whiteSpace: "nowrap", marginLeft: "1rem" }}>
          {node.totalPrice.toFixed(2)} MDL
        </span>
      </div>
    );
  }

  return (
    <div style={{ marginBottom: "4px" }}>
      <button
        onClick={() => setOpen((o) => !o)}
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          width: "100%",
          background: depth === 0 ? "#1a1a2e" : "#161625",
          border: "none",
          borderRadius: "6px",
          padding: "0.45rem 0.7rem",
          cursor: "pointer",
          color: "#ddd",
          fontWeight: depth === 0 ? 700 : 600,
          fontSize: depth === 0 ? "0.95rem" : "0.875rem",
          textAlign: "left",
        }}
      >
        <span>
          {open ? "▾" : "▸"} {node.name}
          {node.description && (
            <span style={{ color: "#555", fontWeight: 400, fontSize: "0.78rem", marginLeft: "0.5rem" }}>
              {node.description}
            </span>
          )}
        </span>
        <span style={{ color: "#ffb74d", whiteSpace: "nowrap", marginLeft: "1rem" }}>
          {node.totalPrice.toFixed(2)} MDL
        </span>
      </button>

      {open && (
        <div style={{ marginLeft: "1rem", marginTop: "3px" }}>
          {node.children!.map((child, i) => (
            <MenuNode key={i} node={child} depth={depth + 1} />
          ))}
        </div>
      )}
    </div>
  );
};

const CompositeSection = () => {
  const [menu, setMenu] = useState<MenuItemDto | null>(null);
  const [filtered, setFiltered] = useState<{ count: number; items: MenuItemDto[] } | null>(null);
  const [minPrice, setMinPrice] = useState("50");
  const [maxPrice, setMaxPrice] = useState("100");
  const [loading, setLoading] = useState<"full" | "filter" | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadFull = async () => {
    setError(null);
    setFiltered(null);
    setLoading("full");
    try {
      setMenu(await getRestaurantMenu());
    } catch {
      setError("Failed to load menu.");
    } finally {
      setLoading(null);
    }
  };

  const loadFiltered = async () => {
    setError(null);
    setMenu(null);
    setLoading("filter");
    try {
      setFiltered(await filterMenu(parseFloat(minPrice) || 0, parseFloat(maxPrice) || 9999));
    } catch {
      setError("Failed to filter menu.");
    } finally {
      setLoading(null);
    }
  };

  return (
    <SectionCard
      title="Restaurant Menu"
      badge="Composite"
      badgeColor="#2e7d32"
      description="Every node – whether a single MenuItem leaf or a MenuSection composite – implements the same IMenuComponent interface. Prices bubble up automatically from leaves to sections."
    >
      <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap", marginBottom: "1rem" }}>
        <button onClick={loadFull} disabled={loading === "full"} style={btnStyle(loading === "full", "#2e7d32")}>
          {loading === "full" ? "Loading…" : "Load full menu"}
        </button>

        <div style={{ display: "flex", alignItems: "center", gap: "0.4rem" }}>
          <input
            style={{ ...inputStyle, width: "80px" }}
            type="number"
            value={minPrice}
            onChange={(e) => setMinPrice(e.target.value)}
            placeholder="Min MDL"
          />
          <span style={{ color: "#666" }}>–</span>
          <input
            style={{ ...inputStyle, width: "80px" }}
            type="number"
            value={maxPrice}
            onChange={(e) => setMaxPrice(e.target.value)}
            placeholder="Max MDL"
          />
          <button onClick={loadFiltered} disabled={loading === "filter"} style={btnStyle(loading === "filter", "#1565c0")}>
            {loading === "filter" ? "…" : "Filter by price"}
          </button>
        </div>
      </div>

      {error && <p style={{ color: "#ef5350" }}>{error}</p>}

      {menu && (
        <div style={{ maxHeight: "420px", overflowY: "auto", paddingRight: "4px" }}>
          <MenuNode node={menu} depth={0} />
        </div>
      )}

      {filtered && (
        <ResultBox>
          <strong style={{ color: "#aaa" }}>
            {filtered.count} items between {minPrice}–{maxPrice} MDL
          </strong>
          <div style={{ marginTop: "0.5rem" }}>
            {filtered.items.map((item, i) => (
              <div
                key={i}
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  padding: "0.3rem 0",
                  borderBottom: "1px solid #1a1a2e",
                  color: "#ccc",
                }}
              >
                <span>{item.name}</span>
                <span style={{ color: "#81c784" }}>{item.totalPrice.toFixed(2)} MDL</span>
              </div>
            ))}
          </div>
        </ResultBox>
      )}
    </SectionCard>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 3.  FAÇADE – Hotel Reservation
// ─────────────────────────────────────────────────────────────────────────────

const ROOM_CODES = [
  { code: "STD", label: "Standard" },
  { code: "DLX", label: "Deluxe" },
  { code: "STE", label: "Suite" },
  { code: "FAM", label: "Family" },
];

const FacadeSection = () => {
  const tomorrow = new Date(Date.now() + 86400000).toISOString().split("T")[0];
  const dayAfter = new Date(Date.now() + 86400000 * 3).toISOString().split("T")[0];

  const [rooms, setRooms] = useState<RoomInfo[]>([]);
  const [searchCheckIn, setSearchCheckIn] = useState(tomorrow);
  const [searchCheckOut, setSearchCheckOut] = useState(dayAfter);
  const [guestCount, setGuestCount] = useState("2");

  const [bookName, setBookName] = useState("Ion Popescu");
  const [bookEmail, setBookEmail] = useState("ion.popescu@email.com");
  const [bookPhone, setBookPhone] = useState("+40-721-123456");
  const [roomCode, setRoomCode] = useState("DLX");
  const [bookGuests, setBookGuests] = useState("2");
  const [checkIn, setCheckIn] = useState(tomorrow);
  const [checkOut, setCheckOut] = useState(dayAfter);
  const [payMethod, setPayMethod] = useState("Credit Card");

  const [confirmation, setConfirmation] = useState<BookingConfirmation | null>(null);
  const [cancelRef, setCancelRef] = useState("");
  const [cancelMsg, setCancelMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState<"search" | "book" | "cancel" | null>(null);
  const [error, setError] = useState<string | null>(null);

  const doSearch = async () => {
    setError(null);
    setLoading("search");
    try {
      setRooms(await searchRooms(searchCheckIn, searchCheckOut, parseInt(guestCount) || 1));
    } catch {
      setError("Room search failed.");
    } finally {
      setLoading(null);
    }
  };

  const doBook = async () => {
    setError(null);
    setConfirmation(null);
    setLoading("book");
    try {
      const c = await bookRoom({
        guestName: bookName,
        guestEmail: bookEmail,
        guestPhone: bookPhone || undefined,
        roomTypeCode: roomCode,
        guestCount: parseInt(bookGuests) || 1,
        checkIn,
        checkOut,
        paymentMethod: payMethod,
      });
      setConfirmation(c);
      setCancelRef(c.bookingReference);
    } catch (e: unknown) {
      const msg =
        e && typeof e === "object" && "response" in e
          ? ((e as { response?: { data?: { error?: string } } }).response?.data?.error ?? "Booking failed.")
          : "Booking failed.";
      setError(msg);
    } finally {
      setLoading(null);
    }
  };

  const doCancel = async () => {
    if (!cancelRef.trim()) return;
    setError(null);
    setCancelMsg(null);
    setLoading("cancel");
    try {
      await cancelBooking(cancelRef.trim());
      setCancelMsg(`Booking #${cancelRef} cancelled successfully.`);
      setConfirmation(null);
    } catch {
      setError("Cancellation failed – reference not found.");
    } finally {
      setLoading(null);
    }
  };

  return (
    <SectionCard
      title="Hotel Reservation"
      badge="Façade"
      badgeColor="#6a1b9a"
      description="BookRoom() is a single call that internally coordinates 5 subsystems: RoomSearch, Availability, Payment, Notifications, and BookingRepository. The caller never touches any of them."
    >
      {/* Search */}
      <p style={{ color: "#aaa", fontSize: "0.8rem", marginBottom: "0.5rem", fontWeight: 600 }}>
        SEARCH ROOMS
      </p>
      <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap", alignItems: "flex-end", marginBottom: "1rem" }}>
        <Field label="Check-in">
          <input style={{ ...inputStyle, width: "140px" }} type="date" value={searchCheckIn} onChange={(e) => setSearchCheckIn(e.target.value)} />
        </Field>
        <Field label="Check-out">
          <input style={{ ...inputStyle, width: "140px" }} type="date" value={searchCheckOut} onChange={(e) => setSearchCheckOut(e.target.value)} />
        </Field>
        <Field label="Guests">
          <input style={{ ...inputStyle, width: "70px" }} type="number" min={1} value={guestCount} onChange={(e) => setGuestCount(e.target.value)} />
        </Field>
        <button onClick={doSearch} disabled={loading === "search"} style={{ ...btnStyle(loading === "search", "#6a1b9a"), marginBottom: "0.75rem" }}>
          {loading === "search" ? "…" : "Search"}
        </button>
      </div>

      {rooms.length > 0 && (
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: "0.5rem", marginBottom: "1.25rem" }}>
          {rooms.map((r) => (
            <div
              key={r.typeName}
              style={{
                padding: "0.75rem",
                background: "#0e0e1a",
                borderRadius: "8px",
                border: "1px solid #2a2a3a",
                fontSize: "0.82rem",
              }}
            >
              <strong style={{ color: "#ce93d8" }}>{r.typeName}</strong>
              <div style={{ color: "#888", marginTop: "0.3rem" }}>{r.description}</div>
              <div style={{ color: "#81c784", marginTop: "0.3rem" }}>{r.pricePerNight} MDL / night</div>
              <div style={{ color: "#666" }}>Max {r.maxGuests} guests</div>
            </div>
          ))}
        </div>
      )}

      {/* Book */}
      <p style={{ color: "#aaa", fontSize: "0.8rem", marginBottom: "0.5rem", fontWeight: 600 }}>
        BOOK A ROOM
      </p>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))", gap: "0.5rem" }}>
        <Field label="Guest name"><input style={inputStyle} value={bookName} onChange={(e) => setBookName(e.target.value)} /></Field>
        <Field label="Email"><input style={inputStyle} type="email" value={bookEmail} onChange={(e) => setBookEmail(e.target.value)} /></Field>
        <Field label="Phone (optional)"><input style={inputStyle} value={bookPhone} onChange={(e) => setBookPhone(e.target.value)} /></Field>
        <Field label="Room type">
          <select style={inputStyle} value={roomCode} onChange={(e) => setRoomCode(e.target.value)}>
            {ROOM_CODES.map((r) => <option key={r.code} value={r.code}>{r.label}</option>)}
          </select>
        </Field>
        <Field label="Guests"><input style={inputStyle} type="number" min={1} value={bookGuests} onChange={(e) => setBookGuests(e.target.value)} /></Field>
        <Field label="Check-in"><input style={inputStyle} type="date" value={checkIn} onChange={(e) => setCheckIn(e.target.value)} /></Field>
        <Field label="Check-out"><input style={inputStyle} type="date" value={checkOut} onChange={(e) => setCheckOut(e.target.value)} /></Field>
        <Field label="Payment method">
          <select style={inputStyle} value={payMethod} onChange={(e) => setPayMethod(e.target.value)}>
            {["Credit Card", "Debit Card", "Bank Transfer"].map((m) => <option key={m}>{m}</option>)}
          </select>
        </Field>
      </div>

      <button onClick={doBook} disabled={loading === "book"} style={{ ...btnStyle(loading === "book", "#6a1b9a"), marginTop: "0.5rem" }}>
        {loading === "book" ? "Booking…" : "Book Room"}
      </button>

      {error && <p style={{ color: "#ef5350", marginTop: "0.75rem" }}>{error}</p>}

      {confirmation && (
        <ResultBox>
          <div style={{ color: "#81c784", fontWeight: 700, marginBottom: "0.5rem" }}>
            ✅ Booking confirmed!
          </div>
          <div style={{ color: "#aaa", lineHeight: 1.8, fontSize: "0.82rem" }}>
            <div><span style={{ color: "#666" }}>Reference: </span><strong style={{ color: "#ce93d8" }}>{confirmation.bookingReference}</strong></div>
            <div><span style={{ color: "#666" }}>Guest: </span>{confirmation.guestName}</div>
            <div><span style={{ color: "#666" }}>Room: </span>{confirmation.roomType}</div>
            <div><span style={{ color: "#666" }}>Dates: </span>{new Date(confirmation.checkIn).toLocaleDateString()} – {new Date(confirmation.checkOut).toLocaleDateString()}</div>
            <div><span style={{ color: "#666" }}>Total: </span><strong style={{ color: "#81c784" }}>{confirmation.totalPrice.toFixed(2)} MDL</strong></div>
          </div>
        </ResultBox>
      )}

      {/* Cancel */}
      <p style={{ color: "#aaa", fontSize: "0.8rem", marginTop: "1.25rem", marginBottom: "0.5rem", fontWeight: 600 }}>
        CANCEL BOOKING
      </p>
      <div style={{ display: "flex", gap: "0.5rem", alignItems: "flex-end" }}>
        <Field label="Booking reference">
          <input
            style={{ ...inputStyle, width: "280px" }}
            value={cancelRef}
            onChange={(e) => setCancelRef(e.target.value)}
            placeholder="BK-…"
          />
        </Field>
        <button onClick={doCancel} disabled={loading === "cancel" || !cancelRef.trim()} style={{ ...btnStyle(loading === "cancel" || !cancelRef.trim(), "#c62828"), marginBottom: "0.75rem" }}>
          {loading === "cancel" ? "…" : "Cancel"}
        </button>
      </div>
      {cancelMsg && <p style={{ color: "#81c784", marginTop: "0.5rem" }}>{cancelMsg}</p>}
    </SectionCard>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// ROOT EXPORT
// ─────────────────────────────────────────────────────────────────────────────

const StructuralPatternsDemoSection = () => (
  <section style={{ marginTop: "2.5rem", textAlign: "left" }}>
    <h2 style={{ marginBottom: "0.25rem" }}>Structural Design Patterns</h2>
    <p style={{ color: "#666", fontSize: "0.85rem", marginBottom: "1.5rem" }}>
      Live demos calling backend endpoints that implement Adapter, Composite, and Façade patterns in C#.
    </p>
    <AdapterSection />
    <CompositeSection />
    <FacadeSection />
  </section>
);

export default StructuralPatternsDemoSection;
