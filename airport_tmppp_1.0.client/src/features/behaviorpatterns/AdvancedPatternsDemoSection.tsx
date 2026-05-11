import { useState, useEffect, useCallback } from "react";
import {
  processBooking, getChainContext,
  getAllFlightStates, transitionFlightState, getStateActions,
  coordinateServices, getMediatorLog, getMediatorOperationTypes,
  runFlightOperation, getTemplateOperationTypes,
  generateReport, getReportTypes,
  type BookingResult, type ChainContext, type BookingRequestDto,
  type FlightStateContext, type TransitionResponse,
  type CoordinationResult, type OperationType, type ServiceLogEntry,
  type FlightOperationReport, type TemplateOperationType,
  type VisitorReport, type ReportType,
} from "../../api/advancedPatternsApi";

// ─────────────────────────────────────────────────────────────────────────────
// Design tokens  (matches existing behavioral section palette)
// ─────────────────────────────────────────────────────────────────────────────
const C = {
  bg:       "#0a0a12",
  surface:  "#10101e",
  card:     "#14142a",
  border:   "#1e1e3a",
  txt:      "#e0e0f0",
  muted:    "#6666a0",
  chain:    "#f97316",   // orange
  state:    "#a78bfa",   // violet
  mediator: "#34d399",   // emerald
  template: "#60a5fa",   // blue
  visitor:  "#f472b6",   // pink
  success:  "#4ade80",
  error:    "#f87171",
  warn:     "#fbbf24",
};

const mono = "'JetBrains Mono','Fira Code','Cascadia Code',monospace";

// ─────────────────────────────────────────────────────────────────────────────
// Micro-components
// ─────────────────────────────────────────────────────────────────────────────

const Tag = ({ label, color }: { label: string; color: string }) => (
  <span style={{
    fontSize: "0.65rem", padding: "0.15rem 0.55rem", borderRadius: 999,
    background: `${color}22`, color, border: `1px solid ${color}44`,
    fontWeight: 700, letterSpacing: "0.07em", textTransform: "uppercase" as const,
    fontFamily: mono, whiteSpace: "nowrap" as const,
  }}>{label}</span>
);

const Card = ({ title, badge, color, description, children }: {
  title: string; badge: string; color: string; description: string;
  children: React.ReactNode;
}) => (
  <div style={{
    border: `1px solid ${color}33`, borderRadius: 16, padding: "1.75rem",
    marginBottom: "2.5rem", background: C.card, boxShadow: `0 0 40px ${color}08`,
  }}>
    <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: "0.35rem" }}>
      <h3 style={{ margin: 0, color: C.txt, fontFamily: mono, fontSize: "1.05rem" }}>{title}</h3>
      <Tag label={badge} color={color} />
    </div>
    <p style={{ color: C.muted, fontSize: "0.82rem", margin: "0 0 1.4rem", lineHeight: 1.65 }}>{description}</p>
    {children}
  </div>
);

const Inp = (p: React.InputHTMLAttributes<HTMLInputElement>) => (
  <input {...p} style={{
    background: C.surface, border: `1px solid ${C.border}`, borderRadius: 8,
    color: C.txt, padding: "0.45rem 0.7rem", fontSize: "0.88rem",
    fontFamily: mono, width: "100%", boxSizing: "border-box" as const, outline: "none",
    ...p.style,
  }} />
);

const Sel = (p: React.SelectHTMLAttributes<HTMLSelectElement>) => (
  <select {...p} style={{
    background: C.surface, border: `1px solid ${C.border}`, borderRadius: 8,
    color: C.txt, padding: "0.45rem 0.7rem", fontSize: "0.88rem",
    fontFamily: mono, width: "100%", boxSizing: "border-box" as const,
    outline: "none", cursor: "pointer", ...p.style,
  }}>{p.children}</select>
);

const Btn = ({ children, onClick, disabled, color, small }: {
  children: React.ReactNode; onClick?: () => void;
  disabled?: boolean; color?: string; small?: boolean;
}) => (
  <button onClick={onClick} disabled={disabled} style={{
    padding: small ? "0.32rem 0.75rem" : "0.5rem 1.15rem",
    background: disabled ? C.surface : `${color ?? C.muted}22`,
    color: disabled ? C.muted : (color ?? C.txt),
    border: `1px solid ${disabled ? C.border : (color ?? C.muted) + "55"}`,
    borderRadius: 8, cursor: disabled ? "not-allowed" : "pointer",
    fontWeight: 700, fontSize: small ? "0.76rem" : "0.87rem",
    fontFamily: mono, transition: "all 0.15s", whiteSpace: "nowrap" as const,
  }}>{children}</button>
);

const Pill = ({ val, active, onClick, color }: {
  val: string; active: boolean; onClick: () => void; color: string;
}) => (
  <button onClick={onClick} style={{
    padding: "0.3rem 0.85rem", borderRadius: 999,
    background: active ? `${color}30` : "transparent",
    border: `1px solid ${active ? color : C.border}`,
    color: active ? color : C.muted, cursor: "pointer",
    fontFamily: mono, fontSize: "0.78rem", fontWeight: active ? 700 : 400,
    transition: "all 0.15s",
  }}>{val}</button>
);

const Box = ({ children, color }: { children: React.ReactNode; color?: string }) => (
  <div style={{
    marginTop: "1rem", padding: "0.9rem 1rem", background: C.surface,
    border: `1px solid ${color ? color + "33" : C.border}`, borderRadius: 10,
    fontSize: "0.82rem", fontFamily: mono, lineHeight: 1.75,
  }}>{children}</div>
);

const Fld = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div style={{ marginBottom: "0.65rem" }}>
    <div style={{ color: C.muted, fontSize: "0.73rem", marginBottom: "0.22rem", fontFamily: mono }}>
      {label}
    </div>
    {children}
  </div>
);

const G = ({ cols, children, gap = "0.6rem" }: {
  cols: string; children: React.ReactNode; gap?: string;
}) => (
  <div style={{ display: "grid", gridTemplateColumns: cols, gap }}>{children}</div>
);

const Divider = () => (
  <div style={{ borderTop: `1px solid ${C.border}`, margin: "0.5rem 0" }} />
);

const statusPalette: Record<string, string> = {
  Scheduled: C.muted, CheckInOpen: C.template, Boarding: C.chain,
  Departed: C.mediator, InAir: C.visitor, Landed: C.success,
  Delayed: C.warn, Cancelled: C.error,
};
const statusColor = (s: string) => statusPalette[s] ?? C.txt;

// ─────────────────────────────────────────────────────────────────────────────
// 1. CHAIN OF RESPONSIBILITY
// ─────────────────────────────────────────────────────────────────────────────

const ChainSection = () => {
  const [ctx, setCtx] = useState<ChainContext | null>(null);
  const [form, setForm] = useState<BookingRequestDto>({
    passengerName: "", passengerEmail: "", flightId: 0, flightNumber: "",
    baggageWeightKg: 20, hasSpecialMeal: false, paymentMethod: "CreditCard", ticketPrice: 350,
  });
  const [result, setResult] = useState<BookingResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError]   = useState<string | null>(null);

  useEffect(() => { getChainContext().then(setCtx).catch(() => {}); }, []);

  const pickFlight = (id: number) => {
    const f = ctx?.flights.find(f => f.id === id);
    if (f) setForm(p => ({ ...p, flightId: f.id, flightNumber: f.flightNumber }));
  };

  const pickPassenger = (email: string) => {
    const p = ctx?.passengers.find(p => p.email === email);
    if (p) setForm(prev => ({
      ...prev, passengerEmail: p.email,
      passengerName: `${p.firstName} ${p.lastName}`,
    }));
  };

  const submit = async () => {
    setError(null); setResult(null); setLoading(true);
    try { setResult(await processBooking(form)); }
    catch (e: unknown) {
      const d = (e as { response?: { data?: BookingResult } })?.response?.data;
      if (d && "chainTrace" in d) setResult(d);
      else setError("Request failed — check console.");
    } finally { setLoading(false); }
  };

  const PAYMENT_METHODS = ["CreditCard", "DebitCard", "BankTransfer", "PayPal", "Cash"];

  return (
    <Card title="Flight Booking Pipeline" badge="Chain of Responsibility" color={C.chain}
      description="A booking request travels through a sequential handler chain — FlightExists → PassengerValidation → BaggageCheck → PaymentValidation → BookingConfirmation. Each handler either passes the request forward or stops the chain.">

      <G cols="1fr 1fr">
        <Fld label="Select flight">
          <Sel value={form.flightId} onChange={e => pickFlight(+e.target.value)}>
            <option value={0}>— choose flight —</option>
            {ctx?.flights.map(f => (
              <option key={f.id} value={f.id}>{f.flightNumber} (ID {f.id})</option>
            ))}
          </Sel>
        </Fld>
        <Fld label="Select passenger">
          <Sel value={form.passengerEmail} onChange={e => pickPassenger(e.target.value)}>
            <option value="">— choose passenger —</option>
            {ctx?.passengers.map(p => (
              <option key={p.id} value={p.email}>{p.firstName} {p.lastName}</option>
            ))}
          </Sel>
        </Fld>
        <Fld label="Baggage weight (kg)">
          <Inp type="number" min={0} max={60} value={form.baggageWeightKg}
            onChange={e => setForm(p => ({ ...p, baggageWeightKg: +e.target.value }))} />
        </Fld>
        <Fld label="Ticket price (MDL)">
          <Inp type="number" min={1} value={form.ticketPrice}
            onChange={e => setForm(p => ({ ...p, ticketPrice: +e.target.value }))} />
        </Fld>
        <Fld label="Payment method">
          <Sel value={form.paymentMethod} onChange={e => setForm(p => ({ ...p, paymentMethod: e.target.value }))}>
            {PAYMENT_METHODS.map(m => <option key={m}>{m}</option>)}
          </Sel>
        </Fld>
        <Fld label="Special meal">
          <div style={{ display: "flex", alignItems: "center", gap: 8, paddingTop: 6 }}>
            <input type="checkbox" checked={form.hasSpecialMeal}
              onChange={e => setForm(p => ({ ...p, hasSpecialMeal: e.target.checked }))}
              style={{ accentColor: C.chain, width: 16, height: 16 }} />
            <span style={{ color: C.muted, fontSize: "0.83rem" }}>Requires special meal</span>
          </div>
        </Fld>
      </G>

      <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.75rem", alignItems: "center" }}>
        <Btn onClick={submit} disabled={loading || !form.flightId || !form.passengerEmail} color={C.chain}>
          {loading ? "Processing…" : "▶ Run Booking Chain"}
        </Btn>
        {/* Sabotage toggles */}
        <Btn onClick={() => setForm(p => ({ ...p, baggageWeightKg: 50 }))} color={C.warn} small>
          ⚠ Over-weight bag (50 kg)
        </Btn>
        <Btn onClick={() => setForm(p => ({ ...p, paymentMethod: "Cash" }))} color={C.error} small>
          ✗ Bad payment
        </Btn>
      </div>

      {error && <p style={{ color: C.error, fontFamily: mono, fontSize: "0.8rem" }}>{error}</p>}

      {result && (
        <Box color={result.success ? C.chain : C.error}>
          {/* Summary */}
          <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: "0.75rem" }}>
            <span style={{ fontSize: "1.1rem" }}>{result.success ? "✅" : "❌"}</span>
            <span style={{ color: result.success ? C.chain : C.error, fontWeight: 700 }}>
              {result.finalMessage}
            </span>
          </div>
          {result.bookingRef && (
            <div style={{ color: C.success, marginBottom: "0.65rem" }}>
              Booking Ref: <strong>{result.bookingRef}</strong>
            </div>
          )}
          <Divider />
          {/* Chain trace */}
          <div style={{ color: C.muted, fontSize: "0.73rem", marginBottom: "0.4rem" }}>
            HANDLER CHAIN TRACE
          </div>
          {result.chainTrace.map((step, i) => (
            <div key={i} style={{
              display: "flex", alignItems: "flex-start", gap: "0.6rem",
              padding: "0.35rem 0", borderBottom: `1px solid ${C.border}`,
            }}>
              <span style={{ fontSize: "0.9rem", minWidth: 18 }}>
                {step.passed ? "✓" : "✗"}
              </span>
              <span style={{ color: step.passed ? C.txt : C.error, minWidth: 200 }}>
                {step.handlerName}
              </span>
              <span style={{ color: C.muted, fontSize: "0.78rem" }}>{step.message}</span>
            </div>
          ))}
        </Box>
      )}
    </Card>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 2. STATE
// ─────────────────────────────────────────────────────────────────────────────

const StateSection = () => {
  const [contexts, setContexts] = useState<FlightStateContext[]>([]);
  const [selected, setSelected] = useState<FlightStateContext | null>(null);
  const [actions, setActions]   = useState<string[]>([]);
  const [actor, setActor]       = useState("Controller");
  const [note, setNote]         = useState("");
  const [resp, setResp]         = useState<TransitionResponse | null>(null);
  const [loading, setLoading]   = useState(false);
  const [error, setError]       = useState<string | null>(null);

  const loadAll = useCallback(async () => {
    try {
      const all = await getAllFlightStates();
      setContexts(all);
      if (selected) {
        const fresh = all.find(c => c.flightId === selected.flightId);
        if (fresh) setSelected(fresh);
      }
    } catch {}
  }, [selected]);

  useEffect(() => {
    loadAll();
    getStateActions().then(setActions).catch(() => {});
  }, []);

  const fire = async (action: string) => {
    if (!selected) return;
    setError(null); setLoading(true);
    try {
      const r = await transitionFlightState(selected.flightId, action, actor || "System", note || undefined);
      setResp(r);
      setSelected(r.context);
      loadAll();
    } catch (e: unknown) {
      const d = (e as { response?: { data?: { message?: string } } })?.response?.data;
      setError(d?.message ?? "Transition failed.");
    } finally { setLoading(false); }
  };

  return (
    <Card title="Flight Lifecycle State Machine" badge="State" color={C.state}
      description="Each flight has a State object that defines which transitions are legal from the current status. The FlightContext delegates every action to its current state — invalid transitions are rejected automatically.">

      {/* Flight grid */}
      <div style={{
        display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(160px, 1fr))",
        gap: "0.5rem", marginBottom: "1rem",
      }}>
        {contexts.map(c => (
          <button key={c.flightId} onClick={() => { setSelected(c); setResp(null); }} style={{
            padding: "0.6rem 0.75rem", borderRadius: 10, cursor: "pointer", textAlign: "left" as const,
            background: selected?.flightId === c.flightId ? `${C.state}22` : C.surface,
            border: `1px solid ${selected?.flightId === c.flightId ? C.state : C.border}`,
            transition: "all 0.15s",
          }}>
            <div style={{ color: C.state, fontFamily: mono, fontWeight: 700, fontSize: "0.85rem" }}>
              {c.flightNumber}
            </div>
            <Tag label={c.statusLabel} color={statusColor(c.currentStatus)} />
          </button>
        ))}
      </div>

      {selected && (
        <>
          {/* Status card */}
          <div style={{
            background: C.surface, borderRadius: 10, padding: "1rem",
            border: `1px solid ${statusColor(selected.currentStatus)}44`,
            marginBottom: "1rem",
          }}>
            <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: "0.4rem" }}>
              <span style={{ color: C.muted, fontFamily: mono, fontSize: "0.8rem" }}>
                Flight {selected.flightNumber} — current state:
              </span>
              <Tag label={selected.statusLabel} color={statusColor(selected.currentStatus)} />
            </div>
            <div style={{ color: C.muted, fontSize: "0.78rem", fontFamily: mono }}>
              {selected.statusDescription}
            </div>
          </div>

          {/* Actor + note */}
          <G cols="1fr 1fr" gap="0.5rem">
            <Fld label="Actor"><Inp value={actor} onChange={e => setActor(e.target.value)} placeholder="e.g. Tower" /></Fld>
            <Fld label="Note (optional)"><Inp value={note} onChange={e => setNote(e.target.value)} placeholder="e.g. Weather cleared" /></Fld>
          </G>

          {/* Allowed transitions */}
          <div style={{ margin: "0.75rem 0 0.5rem", color: C.muted, fontSize: "0.73rem", fontFamily: mono }}>
            ALLOWED TRANSITIONS
          </div>
          <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" as const, marginBottom: "0.75rem" }}>
            {selected.allowedTransitions.length === 0
              ? <span style={{ color: C.error, fontFamily: mono, fontSize: "0.8rem" }}>
                  Terminal state — no further transitions
                </span>
              : selected.allowedTransitions.map(a => (
                  <Btn key={a} onClick={() => fire(a)} disabled={loading} color={C.state}>{a}</Btn>
                ))
            }
          </div>

          {/* All actions (for testing rejections) */}
          <div style={{ color: C.muted, fontSize: "0.73rem", fontFamily: mono, marginBottom: "0.4rem" }}>
            TRY ANY ACTION (illegal ones will be rejected)
          </div>
          <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" as const }}>
            {actions.filter(a => !selected.allowedTransitions.includes(a)).map(a => (
              <Btn key={a} onClick={() => fire(a)} disabled={loading} color={C.muted} small>{a}</Btn>
            ))}
          </div>

          {error && <p style={{ color: C.error, fontFamily: mono, fontSize: "0.8rem" }}>{error}</p>}

          {resp && (
            <Box color={resp.result.success ? C.state : C.error}>
              <div style={{ color: resp.result.success ? C.state : C.error, fontWeight: 700 }}>
                {resp.result.success ? "✓" : "✗"} {resp.result.message}
              </div>
            </Box>
          )}

          {/* History */}
          {selected.history.length > 0 && (
            <Box color={C.state}>
              <div style={{ color: C.state, fontWeight: 700, marginBottom: "0.5rem" }}>Transition History</div>
              {selected.history.slice(0, 6).map((h, i) => (
                <div key={i} style={{
                  display: "flex", gap: "0.75rem", padding: "0.3rem 0",
                  borderBottom: `1px solid ${C.border}`, alignItems: "center",
                  fontSize: "0.78rem",
                }}>
                  <Tag label={h.from} color={statusColor(h.from)} />
                  <span style={{ color: C.muted }}>→</span>
                  <Tag label={h.to} color={statusColor(h.to)} />
                  <span style={{ color: C.muted }}>by {h.actor}</span>
                  {h.note && <span style={{ color: C.muted }}>| {h.note}</span>}
                  <span style={{ color: C.muted, marginLeft: "auto" }}>
                    {new Date(h.occurredAtUtc).toLocaleTimeString()}
                  </span>
                </div>
              ))}
            </Box>
          )}
        </>
      )}
    </Card>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 3. MEDIATOR
// ─────────────────────────────────────────────────────────────────────────────

const MediatorSection = () => {
  const [opTypes, setOpTypes] = useState<OperationType[]>([]);
  const [flights, setFlights] = useState<{ id: number; flightNumber: string }[]>([]);
  const [opType, setOpType]   = useState("FlightDeparture");
  const [flightId, setFlightId] = useState(0);
  const [flightNumber, setFlightNumber] = useState("");
  const [gateCode, setGateCode] = useState("A1");
  const [email, setEmail]     = useState("");
  const [result, setResult]   = useState<CoordinationResult | null>(null);
  const [log, setLog]         = useState<ServiceLogEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [logLoading, setLogLoading] = useState(false);

  useEffect(() => {
    getMediatorOperationTypes().then(setOpTypes).catch(() => {});
    getChainContext().then(c => {
      setFlights(c.flights);
      if (c.flights[0]) { setFlightId(c.flights[0].id); setFlightNumber(c.flights[0].flightNumber); }
    }).catch(() => {});
  }, []);

  const pickFlight = (id: number) => {
    const f = flights.find(f => f.id === id);
    if (f) { setFlightId(f.id); setFlightNumber(f.flightNumber); }
  };

  const coordinate = async () => {
    setLoading(true);
    try {
      setResult(await coordinateServices({ operationType: opType, flightId, flightNumber, gateCode, passengerEmail: email || undefined }));
    } catch { } finally { setLoading(false); }
  };

  const loadLog = async () => {
    setLogLoading(true);
    try { setLog(await getMediatorLog()); } catch { } finally { setLogLoading(false); }
  };

  const currentOp = opTypes.find(o => o.key === opType);
  const svcColor: Record<string, string> = {
    CheckIn: C.template, Security: C.error, GateManagement: C.chain,
    BaggageHandling: C.visitor, AirTrafficControl: C.state,
  };

  return (
    <Card title="Airport Service Coordination Hub" badge="Mediator" color={C.mediator}
      description="Services never call each other directly. Every inter-service request goes through the AirportCoordinationMediator, which routes the operation to the correct set of colleagues and returns a unified result.">

      <G cols="1fr 1fr">
        <Fld label="Operation type">
          <Sel value={opType} onChange={e => setOpType(e.target.value)}>
            {opTypes.map(o => <option key={o.key} value={o.key}>{o.label}</option>)}
          </Sel>
        </Fld>
        <Fld label="Flight">
          <Sel value={flightId} onChange={e => pickFlight(+e.target.value)}>
            {flights.map(f => <option key={f.id} value={f.id}>{f.flightNumber}</option>)}
          </Sel>
        </Fld>
        <Fld label="Gate code"><Inp value={gateCode} onChange={e => setGateCode(e.target.value)} placeholder="A1" /></Fld>
        <Fld label="Passenger email (optional)"><Inp value={email} onChange={e => setEmail(e.target.value)} placeholder="for passenger ops" /></Fld>
      </G>

      {currentOp && (
        <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" as const, margin: "0.5rem 0 0.75rem" }}>
          <span style={{ color: C.muted, fontSize: "0.76rem", fontFamily: mono, alignSelf: "center" }}>Services involved:</span>
          {currentOp.services.map(s => <Tag key={s} label={s} color={svcColor[s] ?? C.muted} />)}
        </div>
      )}

      <div style={{ display: "flex", gap: "0.5rem" }}>
        <Btn onClick={coordinate} disabled={loading || !flightId} color={C.mediator}>
          {loading ? "Coordinating…" : "⚡ Coordinate Services"}
        </Btn>
        <Btn onClick={loadLog} disabled={logLoading} color={C.muted}>
          {logLoading ? "…" : "📋 View Global Log"}
        </Btn>
      </div>

      {result && (
        <Box color={result.success ? C.mediator : C.warn}>
          <div style={{ color: result.success ? C.mediator : C.warn, fontWeight: 700, marginBottom: "0.6rem" }}>
            {result.success ? "✓" : "⚠"} {result.summary}
          </div>
          {result.serviceLog.map((e, i) => (
            <div key={i} style={{
              display: "flex", gap: "0.6rem", padding: "0.35rem 0",
              borderBottom: `1px solid ${C.border}`, alignItems: "center",
            }}>
              <span>{e.success ? "✓" : "✗"}</span>
              <Tag label={e.serviceName} color={svcColor[e.serviceName] ?? C.muted} />
              <span style={{ color: C.muted, fontSize: "0.78rem" }}>{e.message}</span>
            </div>
          ))}
        </Box>
      )}

      {log.length > 0 && (
        <Box color={C.mediator}>
          <div style={{ color: C.mediator, fontWeight: 700, marginBottom: "0.5rem" }}>Recent Service Log</div>
          {log.slice(0, 10).map((e, i) => (
            <div key={i} style={{
              display: "flex", gap: "0.6rem", padding: "0.28rem 0",
              borderBottom: `1px solid ${C.border}`, fontSize: "0.76rem",
            }}>
              <span>{e.success ? "✓" : "✗"}</span>
              <Tag label={e.serviceName} color={svcColor[e.serviceName] ?? C.muted} />
              <span style={{ color: C.muted }}>{e.message}</span>
              <span style={{ color: C.muted, marginLeft: "auto" }}>
                {new Date(e.occurredAtUtc).toLocaleTimeString()}
              </span>
            </div>
          ))}
        </Box>
      )}
    </Card>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 4. TEMPLATE METHOD
// ─────────────────────────────────────────────────────────────────────────────

const TemplateSection = () => {
  const [opTypes, setOpTypes] = useState<TemplateOperationType[]>([]);
  const [flights, setFlights] = useState<{ id: number; flightNumber: string }[]>([]);
  const [opType, setOpType]   = useState("preflight");
  const [flightId, setFlightId] = useState(0);
  const [report, setReport]   = useState<FlightOperationReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError]     = useState<string | null>(null);

  useEffect(() => {
    getTemplateOperationTypes().then(t => { setOpTypes(t); if (t[0]) setOpType(t[0].key); }).catch(() => {});
    getChainContext().then(c => {
      setFlights(c.flights);
      if (c.flights[0]) setFlightId(c.flights[0].id);
    }).catch(() => {});
  }, []);

  const run = async () => {
    setError(null); setLoading(true);
    try { setReport(await runFlightOperation(opType, flightId)); }
    catch (e: unknown) {
      const d = (e as { response?: { data?: FlightOperationReport } })?.response?.data;
      if (d && "steps" in d) setReport(d);
      else setError("Operation failed.");
    } finally { setLoading(false); }
  };

  const opColors: Record<string, string> = {
    preflight: C.template, postflight: C.mediator, emergency: C.error,
  };

  const currentOp = opTypes.find(o => o.key === opType);

  return (
    <Card title="Flight Operation Procedures" badge="Template Method" color={C.template}
      description="FlightOperationTemplate defines an invariant skeleton: Validate → PreCheck → CoreOperation → PostProcedure → Log. Subclasses (PreFlight, PostFlight, Emergency) override the hook steps while the invariant steps always run.">

      {/* Operation selector */}
      <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" as const, marginBottom: "1rem" }}>
        {opTypes.map(o => (
          <Pill key={o.key} val={o.label} active={opType === o.key}
            onClick={() => setOpType(o.key)} color={opColors[o.key] ?? C.template} />
        ))}
      </div>

      {currentOp && (
        <p style={{ color: C.muted, fontSize: "0.78rem", fontFamily: mono, margin: "0 0 0.9rem" }}>
          ↳ {currentOp.description}
        </p>
      )}

      <Fld label="Select flight">
        <Sel value={flightId} onChange={e => setFlightId(+e.target.value)} style={{ maxWidth: 280 }}>
          {flights.map(f => <option key={f.id} value={f.id}>{f.flightNumber} (ID {f.id})</option>)}
        </Sel>
      </Fld>

      <div style={{ marginTop: "0.75rem" }}>
        <Btn onClick={run} disabled={loading || !flightId} color={opColors[opType] ?? C.template}>
          {loading ? "Running…" : "▶ Execute Operation"}
        </Btn>
      </div>

      {error && <p style={{ color: C.error, fontFamily: mono, fontSize: "0.8rem" }}>{error}</p>}

      {report && (
        <Box color={report.overallSuccess ? (opColors[opType] ?? C.template) : C.error}>
          <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: "0.7rem" }}>
            <span>{report.overallSuccess ? "✅" : "❌"}</span>
            <span style={{ color: report.overallSuccess ? (opColors[opType] ?? C.template) : C.error, fontWeight: 700 }}>
              {report.summary}
            </span>
          </div>
          <Divider />
          <div style={{ color: C.muted, fontSize: "0.72rem", margin: "0.4rem 0" }}>STEP TRACE</div>
          {report.steps.map((step, i) => {
            const isInvariant = step.stepName === "FlightValidation" || step.stepName === "CompletionLog";
            return (
              <div key={i} style={{
                display: "flex", alignItems: "flex-start", gap: "0.6rem",
                padding: "0.4rem 0.5rem", margin: "0.2rem 0",
                borderRadius: 6,
                background: isInvariant ? `${C.muted}11` : `${opColors[opType] ?? C.template}0a`,
                border: `1px solid ${isInvariant ? C.border : (opColors[opType] ?? C.template) + "22"}`,
              }}>
                <span style={{ fontSize: "0.9rem", minWidth: 18 }}>{step.completed ? "✓" : "✗"}</span>
                <div style={{ flex: 1 }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: "0.2rem" }}>
                    <span style={{ color: step.completed ? C.txt : C.error, fontSize: "0.82rem", fontWeight: 600 }}>
                      {step.stepName}
                    </span>
                    {isInvariant && <Tag label="invariant" color={C.muted} />}
                    {!isInvariant && <Tag label="hook" color={opColors[opType] ?? C.template} />}
                  </div>
                  <span style={{ color: C.muted, fontSize: "0.76rem" }}>{step.result}</span>
                </div>
              </div>
            );
          })}
        </Box>
      )}
    </Card>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 5. VISITOR
// ─────────────────────────────────────────────────────────────────────────────

const VisitorSection = () => {
  const [reportTypes, setReportTypes] = useState<ReportType[]>([]);
  const [selected, setSelected]       = useState("statistics");
  const [report, setReport]           = useState<VisitorReport | null>(null);
  const [loading, setLoading]         = useState(false);

  useEffect(() => {
    getReportTypes().then(t => { setReportTypes(t); if (t[0]) setSelected(t[0].key); }).catch(() => {});
  }, []);

  const run = async (type: string) => {
    setSelected(type); setLoading(true);
    try { setReport(await generateReport(type)); } catch { } finally { setLoading(false); }
  };

  const typeColors: Record<string, string> = {
    statistics: C.visitor, audit: C.warn, contact: C.mediator,
  };

  const statIcons: Record<string, string> = {
    "Total Airports": "🏛", "Total Flights": "✈", "Total Passengers": "👥",
    "Passengers with Phone": "📱", "Passengers without Phone": "📧",
    "Total Seat Capacity": "💺", "Busiest Airport": "🏆", "Max Flights at Airport": "📊",
  };

  return (
    <Card title="Airport Report Generator" badge="Visitor" color={C.visitor}
      description="Three concrete Visitors traverse the same AirportObjectStructure (Airports + Flights + Passengers built from PostgreSQL) without modifying any element class. Each visitor produces a different report from the same data.">

      <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1rem", flexWrap: "wrap" as const }}>
        {reportTypes.map(r => (
          <Pill key={r.key} val={r.label} active={selected === r.key}
            onClick={() => run(r.key)} color={typeColors[r.key] ?? C.visitor} />
        ))}
      </div>

      {reportTypes.find(r => r.key === selected) && (
        <p style={{ color: C.muted, fontSize: "0.78rem", fontFamily: mono, margin: "0 0 0.75rem" }}>
          ↳ {reportTypes.find(r => r.key === selected)!.description}
        </p>
      )}

      <Btn onClick={() => run(selected)} disabled={loading} color={typeColors[selected] ?? C.visitor}>
        {loading ? "Generating…" : "🔍 Generate Report"}
      </Btn>

      {report && (
        <Box color={typeColors[report.reportType.toLowerCase()] ?? C.visitor}>
          <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "0.6rem" }}>
            <Tag label={report.reportType} color={typeColors[report.reportType.toLowerCase()] ?? C.visitor} />
            <span style={{ color: C.muted, fontSize: "0.72rem" }}>
              {new Date(report.generatedAtUtc).toLocaleString()}
            </span>
          </div>

          {report.summary && (
            <div style={{ color: C.muted, fontSize: "0.78rem", marginBottom: "0.6rem", fontStyle: "italic" as const }}>
              {report.summary}
            </div>
          )}

          <Divider />

          {/* Statistics: card grid */}
          {report.reportType === "Statistics" ? (
            <div style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))",
              gap: "0.5rem", marginTop: "0.5rem",
            }}>
              {report.rows.map((row, i) => (
                <div key={i} style={{
                  background: C.card, borderRadius: 8, padding: "0.65rem 0.8rem",
                  border: `1px solid ${C.border}`,
                }}>
                  <div style={{ color: C.muted, fontSize: "0.7rem", marginBottom: "0.25rem" }}>
                    {statIcons[row.label] ?? "•"} {row.label}
                  </div>
                  <div style={{ color: C.visitor, fontWeight: 700, fontSize: "1rem" }}>{row.value}</div>
                </div>
              ))}
            </div>
          ) : (
            /* Audit / Contact: table */
            <div style={{ maxHeight: 320, overflowY: "auto" as const, marginTop: "0.5rem" }}>
              {report.rows.map((row, i) => (
                <div key={i} style={{
                  display: "flex", gap: "0.75rem", padding: "0.35rem 0",
                  borderBottom: `1px solid ${C.border}`, alignItems: "flex-start",
                }}>
                  <span style={{
                    color: typeColors[selected] ?? C.visitor,
                    minWidth: 180, fontSize: "0.78rem", fontWeight: 600,
                  }}>{row.label}</span>
                  <span style={{ color: C.muted, fontSize: "0.76rem" }}>{row.value}</span>
                </div>
              ))}
            </div>
          )}
        </Box>
      )}
    </Card>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Root export
// ─────────────────────────────────────────────────────────────────────────────

const PATTERNS = [
  { color: C.chain,    label: "Chain of Responsibility", desc: "Handler pipeline with pass/stop logic" },
  { color: C.state,    label: "State",                   desc: "Legal transition enforcement per state" },
  { color: C.mediator, label: "Mediator",                desc: "Centralised inter-service coordinator" },
  { color: C.template, label: "Template Method",         desc: "Fixed skeleton, overridable hooks" },
  { color: C.visitor,  label: "Visitor",                 desc: "Reports without modifying elements" },
];

const AdvancedPatternsDemoSection = () => (
  <section style={{ marginTop: "3rem", textAlign: "left" }}>
    <h2 style={{ fontFamily: mono, color: C.txt, marginBottom: "0.25rem" }}>
      Advanced Behavioral Design Patterns
    </h2>
    <p style={{ color: C.muted, fontSize: "0.85rem", marginBottom: "1.5rem" }}>
      Chain of Responsibility · State · Mediator · Template Method · Visitor — all backed by live PostgreSQL data.
    </p>

    {/* Pattern legend */}
    <div style={{
      background: C.surface, border: `1px solid ${C.border}`, borderRadius: 12,
      padding: "1.1rem 1.4rem", marginBottom: "2rem",
      display: "flex", flexWrap: "wrap" as const, gap: "1.1rem",
    }}>
      {PATTERNS.map(p => (
        <div key={p.label} style={{ display: "flex", alignItems: "center", gap: "0.45rem" }}>
          <span style={{ width: 8, height: 8, borderRadius: "50%", background: p.color, display: "inline-block" }} />
          <Tag label={p.label} color={p.color} />
          <span style={{ color: C.muted, fontSize: "0.73rem", fontFamily: mono }}>{p.desc}</span>
        </div>
      ))}
    </div>

    <ChainSection />
    <StateSection />
    <MediatorSection />
    <TemplateSection />
    <VisitorSection />
  </section>
);

export default AdvancedPatternsDemoSection;
