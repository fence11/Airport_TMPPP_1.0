import { useState } from "react";
import { createReservation, type ReservationSummary, type TransportType, type TicketClass } from "../../api/reservationsApi";

const TRANSPORT_OPTIONS: { value: TransportType; label: string }[] = [
    { value: 0, label: "Airplane" },
    { value: 1, label: "Train" },
    { value: 2, label: "Bus" },
];

const TICKET_OPTIONS: { value: TicketClass; label: string }[] = [
    { value: 0, label: "Economy" },
    { value: 1, label: "First Class" },
];

const TransportReservationSection = () => {
    const [transportType, setTransportType] = useState<TransportType>(0);
    const [ticketClass, setTicketClass] = useState<TicketClass>(0);
    const [customerName, setCustomerName] = useState("");
    const [distanceKm, setDistanceKm] = useState("");
    const [result, setResult] = useState<ReservationSummary | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setResult(null);
        const distance = parseInt(distanceKm, 10);
        if (!customerName.trim()) {
            setError("Customer name is required.");
            return;
        }
        if (Number.isNaN(distance) || distance <= 0) {
            setError("Distance must be a positive number.");
            return;
        }
        setLoading(true);
        try {
            const summary = await createReservation({
                transportType,
                ticketClass,
                customerName: customerName.trim(),
                distanceKm: distance,
            });
            setResult(summary);
        } catch (err: unknown) {
            const message = err && typeof err === "object" && "message" in err
                ? String((err as { message: unknown }).message)
                : "Request failed.";
            setError(message);
            console.error("[Reservations] Error:", err);
        } finally {
            setLoading(false);
        }
    };

    return (
        <section className="card" style={{ marginTop: "2rem", textAlign: "left", maxWidth: "400px" }}>
            <h2>Transport reservation (Factory)</h2>
            <p style={{ color: "#888", fontSize: "0.9rem" }}>
                Choose transport type — the server uses a factory to create the right reservation and calculate price.
            </p>
            <form onSubmit={handleSubmit}>
                <div style={{ marginBottom: "1rem" }}>
                    <label htmlFor="transport-type" style={{ display: "block", marginBottom: "0.25rem" }}>
                        Transport type
                    </label>
                    <select
                        id="transport-type"
                        value={transportType}
                        onChange={(e) => setTransportType(Number(e.target.value) as TransportType)}
                        style={{ width: "100%", padding: "0.5rem" }}
                    >
                        {TRANSPORT_OPTIONS.map((opt) => (
                            <option key={opt.value} value={opt.value}>
                                {opt.label}
                            </option>
                        ))}
                    </select>
                </div>
                <div style={{ marginBottom: "1rem" }}>
                    <label htmlFor="ticket-class" style={{ display: "block", marginBottom: "0.25rem" }}>
                        Ticket class
                    </label>
                    <select
                        id="ticket-class"
                        value={ticketClass}
                        onChange={(e) => setTicketClass(Number(e.target.value) as TicketClass)}
                        style={{ width: "100%", padding: "0.5rem" }}
                    >
                        {TICKET_OPTIONS.map((opt) => (
                            <option key={opt.value} value={opt.value}>
                                {opt.label}
                            </option>
                        ))}
                    </select>
                </div>
                <div style={{ marginBottom: "1rem" }}>
                    <label htmlFor="customer-name" style={{ display: "block", marginBottom: "0.25rem" }}>
                        Customer name
                    </label>
                    <input
                        id="customer-name"
                        type="text"
                        value={customerName}
                        onChange={(e) => setCustomerName(e.target.value)}
                        placeholder="e.g. Jane Doe"
                        style={{ width: "100%", padding: "0.5rem", boxSizing: "border-box" }}
                    />
                </div>
                <div style={{ marginBottom: "1rem" }}>
                    <label htmlFor="distance-km" style={{ display: "block", marginBottom: "0.25rem" }}>
                        Distance (km)
                    </label>
                    <input
                        id="distance-km"
                        type="number"
                        min={1}
                        value={distanceKm}
                        onChange={(e) => setDistanceKm(e.target.value)}
                        placeholder="e.g. 300"
                        style={{ width: "100%", padding: "0.5rem", boxSizing: "border-box" }}
                    />
                </div>
                <button type="submit" disabled={loading} style={{ padding: "0.5rem 1rem" }}>
                    {loading ? "Creating…" : "Create reservation"}
                </button>
            </form>
            {error && (
                <p style={{ color: "#c00", marginTop: "1rem" }}>{error}</p>
            )}
            {result && (
                <div style={{ marginTop: "1rem", padding: "1rem", background: "#1a1a2e", borderRadius: "8px" }}>
                    <strong>Reservation result (from server)</strong>
                    <ul style={{ margin: "0.5rem 0 0", paddingLeft: "1.25rem" }}>
                        <li>Customer: {result.customerName}</li>
                        <li>Transport: {result.transportName}</li>
                        <li>Distance: {result.distanceKm} km</li>
                        <li>Price: {result.price.toFixed(2)}</li>
                        <li>Description: {result.transportDescription}</li>
                    </ul>
                </div>
            )}
        </section>
    );
};

export default TransportReservationSection;
