import { useState } from "react";
import {
  getWeekendCityBreak,
  getBeachHoliday,
  getPrototypeDemo,
  getSingletonInfo,
  type TravelPackage,
  type PrototypeDemoResponse,
  type DatabaseConnectionInfo,
} from "../../api/designPatternsApi";

const DesignPatternsDemoSection = () => {
  const [customerName, setCustomerName] = useState("Demo Passenger");
  const [city, setCity] = useState("Paris");
  const [resort, setResort] = useState("Maldives");

  const [builderResult, setBuilderResult] = useState<TravelPackage | null>(null);
  const [prototypeResult, setPrototypeResult] = useState<PrototypeDemoResponse | null>(null);
  const [singletonResult, setSingletonResult] = useState<DatabaseConnectionInfo | null>(null);

  const [loading, setLoading] = useState<null | "builder-weekend" | "builder-beach" | "prototype" | "singleton">(null);
  const [error, setError] = useState<string | null>(null);

  const runBuilderWeekend = async () => {
    setError(null);
    setBuilderResult(null);
    if (!customerName.trim() || !city.trim()) {
      setError("Customer name and city are required for the Builder demo.");
      return;
    }
    setLoading("builder-weekend");
    try {
      const result = await getWeekendCityBreak(customerName.trim(), city.trim());
      setBuilderResult(result);
    } catch (e) {
      setError("Failed to load Builder (weekend) demo.");
      console.error("[DesignPatterns] Builder weekend error:", e);
    } finally {
      setLoading(null);
    }
  };

  const runBuilderBeach = async () => {
    setError(null);
    setBuilderResult(null);
    if (!customerName.trim() || !resort.trim()) {
      setError("Customer name and resort are required for the Builder demo.");
      return;
    }
    setLoading("builder-beach");
    try {
      const result = await getBeachHoliday(customerName.trim(), resort.trim());
      setBuilderResult(result);
    } catch (e) {
      setError("Failed to load Builder (beach) demo.");
      console.error("[DesignPatterns] Builder beach error:", e);
    } finally {
      setLoading(null);
    }
  };

  const runPrototype = async () => {
    setError(null);
    setPrototypeResult(null);
    setLoading("prototype");
    try {
      const result = await getPrototypeDemo();
      setPrototypeResult(result);
    } catch (e) {
      setError("Failed to load Prototype demo.");
      console.error("[DesignPatterns] Prototype error:", e);
    } finally {
      setLoading(null);
    }
  };

  const runSingleton = async () => {
    setError(null);
    setLoading("singleton");
    try {
      const result = await getSingletonInfo();
      setSingletonResult(result);
    } catch (e) {
      setError("Failed to load Singleton demo.");
      console.error("[DesignPatterns] Singleton error:", e);
    } finally {
      setLoading(null);
    }
  };

  return (
    <section className="card" style={{ marginTop: "2rem", textAlign: "left" }}>
      <h2>Creational design patterns (live demo)</h2>
      <p style={{ color: "#888", fontSize: "0.9rem", marginBottom: "1.5rem" }}>
        These examples call backend endpoints that use Builder, Prototype, and Singleton patterns in your C# code.
      </p>

      {error && <p style={{ color: "#c00", marginBottom: "1rem" }}>{error}</p>}

      {/* Builder */}
      <div style={{ marginBottom: "2rem" }}>
        <h3>Builder – travel package</h3>
        <p style={{ color: "#aaa", fontSize: "0.85rem" }}>
          The server uses a director and a builder to construct immutable travel packages.
        </p>
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))",
            gap: "0.75rem",
            marginTop: "0.75rem",
          }}
        >
          <div>
            <label style={{ display: "block", marginBottom: "0.25rem" }}>Customer name</label>
            <input
              type="text"
              value={customerName}
              onChange={(e) => setCustomerName(e.target.value)}
              style={{ width: "100%", padding: "0.5rem", boxSizing: "border-box" }}
            />
          </div>
          <div>
            <label style={{ display: "block", marginBottom: "0.25rem" }}>City (weekend)</label>
            <input
              type="text"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              style={{ width: "100%", padding: "0.5rem", boxSizing: "border-box" }}
            />
          </div>
          <div>
            <label style={{ display: "block", marginBottom: "0.25rem" }}>Resort (beach)</label>
            <input
              type="text"
              value={resort}
              onChange={(e) => setResort(e.target.value)}
              style={{ width: "100%", padding: "0.5rem", boxSizing: "border-box" }}
            />
          </div>
        </div>
        <div style={{ marginTop: "0.75rem", display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
          <button
            type="button"
            onClick={runBuilderWeekend}
            disabled={loading === "builder-weekend"}
            style={{ padding: "0.5rem 1rem" }}
          >
            {loading === "builder-weekend" ? "Loading…" : "Weekend city break"}
          </button>
          <button
            type="button"
            onClick={runBuilderBeach}
            disabled={loading === "builder-beach"}
            style={{ padding: "0.5rem 1rem" }}
          >
            {loading === "builder-beach" ? "Loading…" : "Beach holiday"}
          </button>
        </div>

        {builderResult && (
          <div
            style={{
              marginTop: "1rem",
              padding: "1rem",
              background: "#1a1a2e",
              borderRadius: "8px",
            }}
          >
            <strong>{builderResult.demoTitle}</strong>
            <ul style={{ margin: "0.5rem 0 0", paddingLeft: "1.25rem" }}>
              <li>Customer: {builderResult.customerName}</li>
              <li>Destination: {builderResult.destination}</li>
              <li>
                Dates: {new Date(builderResult.startDate).toLocaleDateString()} –{" "}
                {new Date(builderResult.endDate).toLocaleDateString()}
              </li>
              <li>Transport: {builderResult.transport}</li>
              <li>Accommodation: {builderResult.accommodation}</li>
              <li>Activities: {builderResult.activities.join(", ") || "None"}</li>
              <li>Price: {builderResult.price.toFixed(2)}</li>
            </ul>
          </div>
        )}
      </div>

      {/* Prototype */}
      <div style={{ marginBottom: "2rem" }}>
        <h3>Prototype – travel document</h3>
        <p style={{ color: "#aaa", fontSize: "0.85rem" }}>
          The server creates a base travel document and clones it. The clone is then customized, while the original
          remains unchanged.
        </p>
        <button
          type="button"
          onClick={runPrototype}
          disabled={loading === "prototype"}
          style={{ padding: "0.5rem 1rem", marginTop: "0.5rem" }}
        >
          {loading === "prototype" ? "Loading…" : "Run prototype demo"}
        </button>
        {prototypeResult && (
          <div
            style={{
              marginTop: "1rem",
              display: "grid",
              gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
              gap: "0.75rem",
            }}
          >
            <div style={{ padding: "1rem", background: "#1a1a2e", borderRadius: "8px" }}>
              <strong>Original</strong>
              <ul style={{ margin: "0.5rem 0 0", paddingLeft: "1.25rem" }}>
                <li>Title: {prototypeResult.original.title}</li>
                <li>Passenger: {prototypeResult.original.passengerName}</li>
                <li>Destination: {prototypeResult.original.destination}</li>
                <li>
                  Travel date: {new Date(prototypeResult.original.travelDate).toLocaleDateString()}
                </li>
                <li>Notes: {prototypeResult.original.notes.join(", ") || "None"}</li>
              </ul>
            </div>
            <div style={{ padding: "1rem", background: "#1a1a2e", borderRadius: "8px" }}>
              <strong>Clone</strong>
              <ul style={{ margin: "0.5rem 0 0", paddingLeft: "1.25rem" }}>
                <li>Title: {prototypeResult.clone.title}</li>
                <li>Passenger: {prototypeResult.clone.passengerName}</li>
                <li>Destination: {prototypeResult.clone.destination}</li>
                <li>
                  Travel date: {new Date(prototypeResult.clone.travelDate).toLocaleDateString()}
                </li>
                <li>Notes: {prototypeResult.clone.notes.join(", ") || "None"}</li>
              </ul>
            </div>
          </div>
        )}
      </div>

      {/* Singleton */}
      <div>
        <h3>Singleton – shared connection manager</h3>
        <p style={{ color: "#aaa", fontSize: "0.85rem" }}>
          All requests share the same C# singleton instance. Repeated calls here should show the same connection id while
          the last-used time updates.
        </p>
        <button
          type="button"
          onClick={runSingleton}
          disabled={loading === "singleton"}
          style={{ padding: "0.5rem 1rem", marginTop: "0.5rem" }}
        >
          {loading === "singleton" ? "Loading…" : "Ping singleton"}
        </button>
        {singletonResult && (
          <div
            style={{
              marginTop: "1rem",
              padding: "1rem",
              background: "#1a1a2e",
              borderRadius: "8px",
            }}
          >
            <ul style={{ margin: 0, paddingLeft: "1.25rem" }}>
              <li>Connection id: {singletonResult.connectionId}</li>
              <li>Last used (UTC): {new Date(singletonResult.lastUsedUtc).toISOString()}</li>
            </ul>
          </div>
        )}
      </div>
    </section>
  );
};

export default DesignPatternsDemoSection;

