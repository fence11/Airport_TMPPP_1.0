import { useState, type CSSProperties } from "react";
import {
  getWeekendCityBreak,
  getBeachHoliday,
  getPrototypeDemo,
  getSingletonInfo,
  getAbstractFactoryBundle,
  type TravelPackage,
  type PrototypeDemoResponse,
  type DatabaseConnectionInfo,
  type AbstractFactoryBundle,
  type TravelerProfile,
} from "../../api/designPatternsApi";
import {
  createReservation,
  type ReservationSummary,
  type TicketClass,
  type TransportType,
} from "../../api/reservationsApi";

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

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div style={{ marginBottom: "0.75rem" }}>
    <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.85rem", color: "#aaa" }}>{label}</label>
    {children}
  </div>
);

const inputStyle: CSSProperties = {
  width: "100%",
  padding: "0.45rem 0.6rem",
  boxSizing: "border-box",
  background: "#1e1e2e",
  border: "1px solid #333",
  borderRadius: "6px",
  color: "#eee",
  fontSize: "0.9rem",
};

const btnStyle = (disabled: boolean, accent = "#5c6bc0"): CSSProperties => ({
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

const CodeCallout = ({
  title,
  lines,
  color,
}: {
  title: string;
  lines: string[];
  color: string;
}) => (
  <div
    style={{
      border: `1px solid ${color}`,
      borderRadius: "8px",
      padding: "0.7rem 0.8rem",
      background: "#0e0e1a",
      marginBottom: "0.65rem",
    }}
  >
    <div style={{ color, fontWeight: 700, marginBottom: "0.4rem", fontSize: "0.82rem" }}>{title}</div>
    <pre style={{ margin: 0, color: "#bbb", fontSize: "0.78rem", whiteSpace: "pre-wrap", lineHeight: 1.45 }}>
      {lines.join("\n")}
    </pre>
  </div>
);

const FactoryMethodSection = () => {
  const [transportType, setTransportType] = useState<TransportType>(0);
  const [ticketClass, setTicketClass] = useState<TicketClass>(0);
  const [customerName, setCustomerName] = useState("Demo Passenger");
  const [distanceKm, setDistanceKm] = useState("300");
  const [result, setResult] = useState<ReservationSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const runDemo = async () => {
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
      const data = await createReservation({
        transportType,
        ticketClass,
        customerName: customerName.trim(),
        distanceKm: distance,
      });
      setResult(data);
    } catch {
      setError("Factory Method demo failed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <SectionCard
      title="Transport Reservation"
      badge="Factory Method"
      badgeColor="#00897b"
      description="The controller selects a reservation factory based on ticket class, and that factory creates the right reservation product for the chosen transport."
    >
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))", gap: "0.5rem" }}>
        <Field label="Ticket class">
          <select style={inputStyle} value={ticketClass} onChange={(e) => setTicketClass(Number(e.target.value) as TicketClass)}>
            <option value={0}>Economy</option>
            <option value={1}>First Class</option>
          </select>
        </Field>
        <Field label="Transport">
          <select style={inputStyle} value={transportType} onChange={(e) => setTransportType(Number(e.target.value) as TransportType)}>
            <option value={0}>Airplane</option>
            <option value={1}>Train</option>
            <option value={2}>Bus</option>
          </select>
        </Field>
        <Field label="Customer">
          <input style={inputStyle} value={customerName} onChange={(e) => setCustomerName(e.target.value)} />
        </Field>
        <Field label="Distance (km)">
          <input style={inputStyle} type="number" min={1} value={distanceKm} onChange={(e) => setDistanceKm(e.target.value)} />
        </Field>
      </div>
      <button onClick={runDemo} disabled={loading} style={{ ...btnStyle(loading, "#00897b"), marginTop: "0.5rem" }}>
        {loading ? "Creating..." : "Run Factory Method"}
      </button>
      {error && <p style={{ color: "#ef5350", marginTop: "0.75rem" }}>{error}</p>}
      {result && (
        <ResultBox>
          <div><span style={{ color: "#666" }}>Customer: </span>{result.customerName}</div>
          <div><span style={{ color: "#666" }}>Transport: </span>{result.transportName}</div>
          <div><span style={{ color: "#666" }}>Distance: </span>{result.distanceKm} km</div>
          <div><span style={{ color: "#666" }}>Price: </span><strong style={{ color: "#81c784" }}>{result.price.toFixed(2)}</strong></div>
        </ResultBox>
      )}
    </SectionCard>
  );
};

const AbstractFactorySection = () => {
  const [profile, setProfile] = useState<TravelerProfile>("business");
  const [result, setResult] = useState<AbstractFactoryBundle | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const runDemo = async (value: TravelerProfile) => {
    setError(null);
    setResult(null);
    setProfile(value);
    setLoading(true);
    try {
      setResult(await getAbstractFactoryBundle(value));
    } catch {
      setError("Abstract Factory demo failed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <SectionCard
      title="Traveler Service Bundle"
      badge="Abstract Factory"
      badgeColor="#3949ab"
      description="A single family factory creates multiple related products together: lounge, transfer, and insurance. Switching profile swaps the whole product family."
    >
      <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
        <button onClick={() => runDemo("business")} disabled={loading} style={btnStyle(loading || profile === "business", "#3949ab")}>
          Business profile
        </button>
        <button onClick={() => runDemo("family")} disabled={loading} style={btnStyle(loading || profile === "family", "#5e35b1")}>
          Family profile
        </button>
      </div>
      {error && <p style={{ color: "#ef5350", marginTop: "0.75rem" }}>{error}</p>}
      {result && (
        <ResultBox>
          <div><span style={{ color: "#666" }}>Lounge: </span>{result.loungeName}</div>
          <div><span style={{ color: "#666" }}>Transfer: </span>{result.transferType}</div>
          <div><span style={{ color: "#666" }}>Insurance: </span>{result.insurancePlan}</div>
          <div style={{ marginTop: "0.45rem", color: "#aaa" }}>{result.loungeBenefits}</div>
          <div style={{ color: "#aaa" }}>{result.transferDetails}</div>
          <div style={{ color: "#aaa" }}>{result.insuranceCoverage}</div>
        </ResultBox>
      )}
    </SectionCard>
  );
};

const BuilderSection = () => {
  const [customerName, setCustomerName] = useState("Demo Passenger");
  const [city, setCity] = useState("Paris");
  const [resort, setResort] = useState("Maldives");
  const [result, setResult] = useState<TravelPackage | null>(null);
  const [loading, setLoading] = useState<"weekend" | "beach" | null>(null);
  const [error, setError] = useState<string | null>(null);

  const runWeekend = async () => {
    setError(null);
    setResult(null);
    if (!customerName.trim() || !city.trim()) {
      setError("Customer and city are required.");
      return;
    }
    setLoading("weekend");
    try {
      setResult(await getWeekendCityBreak(customerName.trim(), city.trim()));
    } catch {
      setError("Builder weekend demo failed.");
    } finally {
      setLoading(null);
    }
  };

  const runBeach = async () => {
    setError(null);
    setResult(null);
    if (!customerName.trim() || !resort.trim()) {
      setError("Customer and resort are required.");
      return;
    }
    setLoading("beach");
    try {
      setResult(await getBeachHoliday(customerName.trim(), resort.trim()));
    } catch {
      setError("Builder beach demo failed.");
    } finally {
      setLoading(null);
    }
  };

  return (
    <SectionCard
      title="Travel Package Builder"
      badge="Builder"
      badgeColor="#c2185b"
      description="Director methods call the same builder steps in different combinations to assemble distinct package variants."
    >
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))", gap: "0.5rem" }}>
        <Field label="Customer"><input style={inputStyle} value={customerName} onChange={(e) => setCustomerName(e.target.value)} /></Field>
        <Field label="City (weekend)"><input style={inputStyle} value={city} onChange={(e) => setCity(e.target.value)} /></Field>
        <Field label="Resort (beach)"><input style={inputStyle} value={resort} onChange={(e) => setResort(e.target.value)} /></Field>
      </div>
      <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
        <button onClick={runWeekend} disabled={loading === "weekend"} style={btnStyle(loading === "weekend", "#d81b60")}>
          {loading === "weekend" ? "Loading..." : "Weekend package"}
        </button>
        <button onClick={runBeach} disabled={loading === "beach"} style={btnStyle(loading === "beach", "#ad1457")}>
          {loading === "beach" ? "Loading..." : "Beach package"}
        </button>
      </div>
      {error && <p style={{ color: "#ef5350", marginTop: "0.75rem" }}>{error}</p>}
      {result && (
        <ResultBox>
          <div><strong>{result.demoTitle}</strong></div>
          <div><span style={{ color: "#666" }}>Destination: </span>{result.destination}</div>
          <div><span style={{ color: "#666" }}>Transport: </span>{result.transport}</div>
          <div><span style={{ color: "#666" }}>Accommodation: </span>{result.accommodation}</div>
          <div><span style={{ color: "#666" }}>Activities: </span>{result.activities.join(", ") || "None"}</div>
          <div><span style={{ color: "#666" }}>Price: </span><strong style={{ color: "#81c784" }}>{result.price.toFixed(2)}</strong></div>
        </ResultBox>
      )}
    </SectionCard>
  );
};

const PrototypeSection = () => {
  const [result, setResult] = useState<PrototypeDemoResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const runDemo = async () => {
    setError(null);
    setResult(null);
    setLoading(true);
    try {
      setResult(await getPrototypeDemo());
    } catch {
      setError("Prototype demo failed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <SectionCard
      title="Travel Document Clone"
      badge="Prototype"
      badgeColor="#7b1fa2"
      description="The server clones a base document and customizes the clone without mutating the original object."
    >
      <button onClick={runDemo} disabled={loading} style={btnStyle(loading, "#7b1fa2")}>
        {loading ? "Cloning..." : "Run Prototype"}
      </button>
      {error && <p style={{ color: "#ef5350", marginTop: "0.75rem" }}>{error}</p>}
      {result && (
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))", gap: "0.75rem", marginTop: "1rem" }}>
          <ResultBox>
            <strong>Original</strong>
            <div><span style={{ color: "#666" }}>Passenger: </span>{result.original.passengerName}</div>
            <div><span style={{ color: "#666" }}>Destination: </span>{result.original.destination}</div>
            <div><span style={{ color: "#666" }}>Notes: </span>{result.original.notes.join(", ") || "None"}</div>
          </ResultBox>
          <ResultBox>
            <strong>Clone</strong>
            <div><span style={{ color: "#666" }}>Passenger: </span>{result.clone.passengerName}</div>
            <div><span style={{ color: "#666" }}>Destination: </span>{result.clone.destination}</div>
            <div><span style={{ color: "#666" }}>Notes: </span>{result.clone.notes.join(", ") || "None"}</div>
          </ResultBox>
        </div>
      )}
    </SectionCard>
  );
};

const SingletonSection = () => {
  const [result, setResult] = useState<DatabaseConnectionInfo | null>(null);
  const [history, setHistory] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const runDemo = async () => {
    setError(null);
    setLoading(true);
    try {
      const response = await getSingletonInfo();
      setResult(response);
      setHistory((prev) => [response.connectionId, ...prev].slice(0, 5));
    } catch {
      setError("Singleton demo failed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <SectionCard
      title="Database Connection Manager"
      badge="Singleton"
      badgeColor="#455a64"
      description="Repeated API calls should report the same instance id, proving one shared object is reused across requests."
    >
      <button onClick={runDemo} disabled={loading} style={btnStyle(loading, "#455a64")}>
        {loading ? "Pinging..." : "Ping Singleton"}
      </button>
      {error && <p style={{ color: "#ef5350", marginTop: "0.75rem" }}>{error}</p>}
      {result && (
        <ResultBox>
          <div><span style={{ color: "#666" }}>Connection id: </span><strong>{result.connectionId}</strong></div>
          <div><span style={{ color: "#666" }}>Last used (UTC): </span>{new Date(result.lastUsedUtc).toISOString()}</div>
          <div style={{ marginTop: "0.45rem", color: "#aaa" }}>
            Last ids: {history.join(" -> ")}
          </div>
        </ResultBox>
      )}
    </SectionCard>
  );
};

const DesignPatternsDemoSection = () => (
  <section style={{ marginTop: "2.5rem", textAlign: "left" }}>
    <h2 style={{ marginBottom: "0.25rem" }}>Creational Design Patterns</h2>
    <p style={{ color: "#666", fontSize: "0.85rem", marginBottom: "1.5rem" }}>
      Live demos calling backend endpoints that implement Factory Method, Abstract Factory, Builder, Prototype, and Singleton.
    </p>

    <ResultBox>
      <div style={{ color: "#ddd", fontWeight: 700, marginBottom: "0.55rem", fontSize: "0.86rem" }}>
        How each pattern works in this codebase
      </div>
      <CodeCallout
        title="Factory Method flow"
        color="#00897b"
        lines={[
          "UI -> createReservation(req)",
          "POST /api/Reservations",
          "Controller picks EconomyReservationFactory or FirstClassReservationFactory",
          "Factory creates reservation product for Airplane/Train/Bus",
        ]}
      />
      <CodeCallout
        title="Abstract Factory flow"
        color="#3949ab"
        lines={[
          "UI -> getAbstractFactoryBundle(profile)",
          "GET /api/DesignPatterns/abstract-factory/service-bundle",
          "BusinessTravelerFactory or FamilyTravelerFactory selected",
          "Same family creates lounge + transfer + insurance together",
        ]}
      />
      <CodeCallout
        title="Builder flow"
        color="#c2185b"
        lines={[
          "UI -> getWeekendCityBreak() or getBeachHoliday()",
          "Director runs a different build sequence on same builder",
          "Builder assembles TravelPackage step by step",
        ]}
      />
      <CodeCallout
        title="Prototype flow"
        color="#7b1fa2"
        lines={[
          "UI -> getPrototypeDemo()",
          "Server creates base document and calls DeepClone()",
          "Clone is customized while original remains intact",
        ]}
      />
      <CodeCallout
        title="Singleton flow"
        color="#455a64"
        lines={[
          "UI -> getSingletonInfo() repeatedly",
          "Server uses DatabaseConnectionManager.Instance",
          "Same connection id is reused, timestamp changes",
        ]}
      />
    </ResultBox>

    <FactoryMethodSection />
    <AbstractFactorySection />
    <BuilderSection />
    <PrototypeSection />
    <SingletonSection />
  </section>
);

export default DesignPatternsDemoSection;

