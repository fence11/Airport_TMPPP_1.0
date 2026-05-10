import { useState, useEffect, useCallback, type CSSProperties } from "react";
import {
  runSchedulingStrategy, getAvailableStrategies,
  changeFlightStatus, getObserverLogs, getFlightStatuses,
  commandCreateFlight, commandDeleteFlight, commandRenameFlight,
  undoLastCommand, getCommandHistory,
  getCurrentConfig, updateConfig, saveSnapshot, restoreSnapshot,
  undoConfig, getConfigHistory,
  iterateFlights, iterateFacilities, getFacilityTypes,
  type ScheduleResult, type StrategyOption,
  type ObserverLogsResponse, type StatusChangeResponse,
  type CommandHistoryResponse, type CommandResponse,
  type AirportConfigSnapshot,
  type IteratorPageResult, type FlightIteratorItem, type AirportFacility,
} from "../../api/behavioralPatternsApi";

// ─────────────────────────────────────────────────────────────────────────────
// Design tokens
// ─────────────────────────────────────────────────────────────────────────────

const C = {
  bg:        "#0a0a12",
  surface:   "#10101e",
  card:      "#14142a",
  border:    "#1e1e3a",
  borderHi:  "#2e2e5a",
  txt:       "#e0e0f0",
  muted:     "#6666a0",
  strategy:  "#00c2a8",
  observer:  "#f59e0b",
  command:   "#6366f1",
  memento:   "#ec4899",
  iterator:  "#22d3ee",
  success:   "#4ade80",
  error:     "#f87171",
  warn:      "#fb923c",
};

const mono = "'JetBrains Mono', 'Fira Code', 'Cascadia Code', monospace";

// ─────────────────────────────────────────────────────────────────────────────
// Tiny shared components
// ─────────────────────────────────────────────────────────────────────────────

const Tag = ({ label, color }: { label: string; color: string }) => (
  <span style={{
    fontSize: "0.65rem", padding: "0.15rem 0.55rem", borderRadius: "999px",
    background: `${color}22`, color, border: `1px solid ${color}44`,
    fontWeight: 700, letterSpacing: "0.07em", textTransform: "uppercase",
    fontFamily: mono,
  }}>{label}</span>
);

const PatternCard = ({ title, badge, color, description, children }: {
  title: string; badge: string; color: string; description: string; children: React.ReactNode;
}) => (
  <div style={{
    border: `1px solid ${color}33`, borderRadius: 16, padding: "1.75rem",
    marginBottom: "2.5rem", background: C.card,
    boxShadow: `0 0 40px ${color}08`,
  }}>
    <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: "0.4rem" }}>
      <h3 style={{ margin: 0, color: C.txt, fontFamily: mono, fontSize: "1.05rem" }}>{title}</h3>
      <Tag label={badge} color={color} />
    </div>
    <p style={{ color: C.muted, fontSize: "0.82rem", margin: "0 0 1.5rem", lineHeight: 1.6 }}>{description}</p>
    {children}
  </div>
);

const Inp = (props: React.InputHTMLAttributes<HTMLInputElement>) => (
  <input {...props} style={{
    background: C.surface, border: `1px solid ${C.border}`, borderRadius: 8,
    color: C.txt, padding: "0.45rem 0.7rem", fontSize: "0.88rem",
    fontFamily: mono, width: "100%", boxSizing: "border-box",
    outline: "none", ...props.style,
  }} />
);

const Sel = (props: React.SelectHTMLAttributes<HTMLSelectElement>) => (
  <select {...props} style={{
    background: C.surface, border: `1px solid ${C.border}`, borderRadius: 8,
    color: C.txt, padding: "0.45rem 0.7rem", fontSize: "0.88rem",
    fontFamily: mono, width: "100%", boxSizing: "border-box",
    outline: "none", cursor: "pointer", ...props.style,
  }}>{props.children}</select>
);

const Btn = ({ children, onClick, disabled, color = C.command, small }: {
  children: React.ReactNode; onClick?: () => void; disabled?: boolean;
  color?: string; small?: boolean;
}) => (
  <button onClick={onClick} disabled={disabled} style={{
    padding: small ? "0.35rem 0.85rem" : "0.5rem 1.2rem",
    background: disabled ? C.surface : `${color}22`,
    color: disabled ? C.muted : color,
    border: `1px solid ${disabled ? C.border : color + "55"}`,
    borderRadius: 8, cursor: disabled ? "not-allowed" : "pointer",
    fontWeight: 700, fontSize: small ? "0.78rem" : "0.88rem",
    fontFamily: mono, transition: "all 0.15s",
  }}>{children}</button>
);

const ResultBox = ({ children, color }: { children: React.ReactNode; color?: string }) => (
  <div style={{
    marginTop: "1rem", padding: "0.9rem 1rem", background: C.surface,
    border: `1px solid ${color ? color + "33" : C.border}`, borderRadius: 10,
    fontSize: "0.82rem", fontFamily: mono, lineHeight: 1.7,
  }}>{children}</div>
);

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div style={{ marginBottom: "0.7rem" }}>
    <div style={{ color: C.muted, fontSize: "0.75rem", marginBottom: "0.25rem", fontFamily: mono }}>{label}</div>
    {children}
  </div>
);

const Grid = ({ cols, children }: { cols: string; children: React.ReactNode }) => (
  <div style={{ display: "grid", gridTemplateColumns: cols, gap: "0.6rem" }}>{children}</div>
);

const LogLine = ({ text, color }: { text: string; color?: string }) => (
  <div style={{
    padding: "0.25rem 0", borderBottom: `1px solid ${C.border}`,
    color: color ?? C.txt, fontSize: "0.76rem", fontFamily: mono,
  }}>{text}</div>
);

const Pill = ({ val, active, onClick, color }: { val: string; active: boolean; onClick: () => void; color: string }) => (
  <button onClick={onClick} style={{
    padding: "0.3rem 0.8rem", borderRadius: 999,
    background: active ? `${color}33` : "transparent",
    border: `1px solid ${active ? color : C.border}`,
    color: active ? color : C.muted, cursor: "pointer", fontFamily: mono,
    fontSize: "0.78rem", fontWeight: active ? 700 : 400, transition: "all 0.15s",
  }}>{val}</button>
);

const statusColor = (s: string): string => ({
  Scheduled: C.muted, Boarding: C.strategy, Departed: C.iterator,
  InAir: C.observer, Landed: C.success, Delayed: C.warn, Cancelled: C.error,
}[s] ?? C.txt);

// ─────────────────────────────────────────────────────────────────────────────
// 1. STRATEGY
// ─────────────────────────────────────────────────────────────────────────────

const StrategySection = () => {
  const [strategies, setStrategies] = useState<StrategyOption[]>([]);
  const [selected, setSelected] = useState("fcfs");
  const [runwayCount, setRunwayCount] = useState(2);
  const [result, setResult] = useState<ScheduleResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { getAvailableStrategies().then(setStrategies).catch(() => {}); }, []);

  const run = async () => {
    setError(null); setLoading(true);
    try { setResult(await runSchedulingStrategy(selected, runwayCount)); }
    catch { setError("Strategy call failed."); }
    finally { setLoading(false); }
  };

  return (
    <PatternCard title="Flight Scheduling" badge="Strategy" color={C.strategy}
      description="Swap scheduling algorithms at runtime without changing the FlightScheduler client. FCFS uses registration order, SJF minimises turnaround, Round-Robin maximises throughput.">

      <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap", marginBottom: "1rem" }}>
        {strategies.map(s => (
          <Pill key={s.key} val={s.name} active={selected === s.key}
            onClick={() => setSelected(s.key)} color={C.strategy} />
        ))}
      </div>

      {strategies.find(s => s.key === selected) && (
        <p style={{ color: C.muted, fontSize: "0.78rem", margin: "0 0 1rem", fontFamily: mono }}>
          ↳ {strategies.find(s => s.key === selected)!.description}
        </p>
      )}

      <Grid cols="auto 1fr auto">
        <span style={{ color: C.muted, fontSize: "0.8rem", alignSelf: "center", fontFamily: mono }}>Runways:</span>
        <input type="range" min={1} max={4} value={runwayCount}
          onChange={e => setRunwayCount(+e.target.value)}
          style={{ accentColor: C.strategy }} />
        <span style={{ color: C.strategy, fontFamily: mono, fontWeight: 700 }}>{runwayCount}</span>
      </Grid>

      <div style={{ marginTop: "1rem" }}>
        <Btn onClick={run} disabled={loading} color={C.strategy}>
          {loading ? "Scheduling…" : "▶ Run Strategy"}
        </Btn>
      </div>

      {error && <p style={{ color: C.error, fontFamily: mono, fontSize: "0.82rem" }}>{error}</p>}

      {result && (
        <ResultBox color={C.strategy}>
          <div style={{ display: "flex", gap: "1.5rem", marginBottom: "0.75rem", flexWrap: "wrap" }}>
            <span><span style={{ color: C.muted }}>Strategy: </span><Tag label={result.strategy} color={C.strategy} /></span>
            <span style={{ color: C.muted }}>Flights: <strong style={{ color: C.txt }}>{result.totalFlights}</strong></span>
            <span style={{ color: C.muted }}>Runways: <strong style={{ color: C.txt }}>{result.totalRunways}</strong></span>
          </div>
          <div style={{ maxHeight: 240, overflowY: "auto" }}>
            {result.schedule.map((sf, i) => (
              <div key={i} style={{
                display: "flex", justifyContent: "space-between", padding: "0.3rem 0",
                borderBottom: `1px solid ${C.border}`, alignItems: "center",
              }}>
                <span style={{ color: C.strategy, minWidth: 80 }}>{sf.flightNumber}</span>
                <span style={{ color: C.muted, fontSize: "0.75rem" }}>RW-{sf.runwayId.toString().padStart(2, "0")}</span>
                <span style={{ color: C.txt, fontSize: "0.75rem" }}>
                  {new Date(sf.scheduledTime).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                </span>
                <span style={{ color: C.muted, fontSize: "0.72rem", maxWidth: 200 }}>{sf.reason}</span>
              </div>
            ))}
          </div>
        </ResultBox>
      )}
    </PatternCard>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 2. OBSERVER
// ─────────────────────────────────────────────────────────────────────────────

const ObserverSection = () => {
  const [flightNumber, setFlightNumber] = useState("RO123");
  const [statuses, setStatuses] = useState<string[]>([]);
  const [newStatus, setNewStatus] = useState("Boarding");
  const [gateCode, setGateCode] = useState("A1");
  const [reason, setReason] = useState("");
  const [response, setResponse] = useState<StatusChangeResponse | null>(null);
  const [logs, setLogs] = useState<ObserverLogsResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [logsLoading, setLogsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { getFlightStatuses().then(setStatuses).catch(() => {}); }, []);

  const fire = async () => {
    setError(null); setLoading(true);
    try {
      const r = await changeFlightStatus(flightNumber, newStatus, undefined, gateCode || undefined, reason || undefined);
      setResponse(r);
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: string } })?.response?.data ?? "Status change failed.";
      setError(typeof msg === "string" ? msg : JSON.stringify(msg));
    } finally { setLoading(false); }
  };

  const loadLogs = async () => {
    setLogsLoading(true);
    try { setLogs(await getObserverLogs()); }
    catch { setError("Log fetch failed."); }
    finally { setLogsLoading(false); }
  };

  const obsColors: Record<string, string> = {
    GateDisplay: C.strategy, PassengerNotify: C.observer,
    ATC: C.iterator, Baggage: C.memento,
  };

  return (
    <PatternCard title="Flight Status Event Bus" badge="Observer" color={C.observer}
      description="When a flight status changes the event bus notifies all subscribed observers simultaneously: Gate Display, Passenger Notifications, ATC, and Baggage System.">

      <Grid cols="repeat(auto-fit, minmax(150px, 1fr))">
        <Field label="Flight number"><Inp value={flightNumber} onChange={e => setFlightNumber(e.target.value)} /></Field>
        <Field label="New status">
          <Sel value={newStatus} onChange={e => setNewStatus(e.target.value)}>
            {statuses.map(s => <option key={s}>{s}</option>)}
          </Sel>
        </Field>
        <Field label="Gate code"><Inp value={gateCode} onChange={e => setGateCode(e.target.value)} placeholder="A1" /></Field>
        <Field label="Reason (optional)"><Inp value={reason} onChange={e => setReason(e.target.value)} placeholder="e.g. Weather delay" /></Field>
      </Grid>

      <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.5rem" }}>
        <Btn onClick={fire} disabled={loading} color={C.observer}>
          {loading ? "Firing…" : "⚡ Publish Status Change"}
        </Btn>
        <Btn onClick={loadLogs} disabled={logsLoading} color={C.muted}>
          {logsLoading ? "…" : "📋 View All Logs"}
        </Btn>
      </div>

      {error && <p style={{ color: C.error, fontFamily: mono, fontSize: "0.82rem" }}>{error}</p>}

      {response && (
        <ResultBox color={C.observer}>
          <div style={{ marginBottom: "0.75rem" }}>
            <span style={{ color: C.observer, fontWeight: 700 }}>✈ {response.event.flightNumber} </span>
            <Tag label={response.event.oldStatus} color={C.muted} />
            <span style={{ color: C.muted, margin: "0 0.4rem" }}>→</span>
            <Tag label={response.event.newStatus} color={statusColor(response.event.newStatus)} />
          </div>
          {Object.entries(response.observerNotifications).map(([id, obs]) => (
            <div key={id} style={{ marginBottom: "0.5rem" }}>
              <span style={{ color: obsColors[id] ?? C.txt, fontWeight: 700, fontSize: "0.78rem" }}>
                [{obs.observerType}]
              </span>
              {obs.log.slice(0, 2).map((l, i) => (
                <div key={i} style={{ color: C.muted, fontSize: "0.74rem", paddingLeft: "1rem" }}>{l}</div>
              ))}
            </div>
          ))}
        </ResultBox>
      )}

      {logs && (
        <ResultBox color={C.observer}>
          <div style={{ color: C.observer, fontWeight: 700, marginBottom: "0.6rem" }}>Observer Logs</div>
          {logs.observers.map(o => (
            <div key={o.observerId} style={{ marginBottom: "0.75rem" }}>
              <div style={{ color: obsColors[o.observerId] ?? C.txt, fontSize: "0.78rem", fontWeight: 700, marginBottom: "0.3rem" }}>
                {o.observerType}
              </div>
              {o.recentLog.length === 0
                ? <div style={{ color: C.muted, fontSize: "0.74rem" }}>No events yet.</div>
                : o.recentLog.map((l, i) => <LogLine key={i} text={l} color={C.muted} />)}
            </div>
          ))}
        </ResultBox>
      )}
    </PatternCard>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 3. COMMAND
// ─────────────────────────────────────────────────────────────────────────────

const CommandSection = () => {
  const [tab, setTab] = useState<"create" | "delete" | "rename">("create");
  const [flightNum, setFlightNum] = useState("ZZ999");
  const [airportId, setAirportId] = useState("1");
  const [delFlightId, setDelFlightId] = useState("1");
  const [renameId, setRenameId] = useState("1");
  const [renameTo, setRenameTo] = useState("RO999");
  const [history, setHistory] = useState<CommandHistoryResponse | null>(null);
  const [lastResp, setLastResp] = useState<CommandResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refreshHistory = useCallback(async () => {
    try { setHistory(await getCommandHistory()); } catch {}
  }, []);

  useEffect(() => { refreshHistory(); }, [refreshHistory]);

  const execute = async () => {
    setError(null); setLoading(true);
    try {
      let r: CommandResponse;
      if (tab === "create") r = await commandCreateFlight(flightNum, +airportId);
      else if (tab === "delete") r = await commandDeleteFlight(+delFlightId);
      else r = await commandRenameFlight(+renameId, renameTo);
      setLastResp(r);
      refreshHistory();
    } catch (e: unknown) {
      const d = (e as { response?: { data?: { message?: string } } })?.response?.data;
      setError(d?.message ?? "Command failed.");
    } finally { setLoading(false); }
  };

  const undo = async () => {
    setError(null); setLoading(true);
    try { setLastResp(await undoLastCommand()); refreshHistory(); }
    catch { setError("Undo failed."); }
    finally { setLoading(false); }
  };

  return (
    <PatternCard title="Flight Management Commands" badge="Command" color={C.command}
      description="Each action is encapsulated as a Command object with execute() and undo() methods. An Invoker maintains a history stack, enabling one-click rollback of any operation.">

      <div style={{ display: "flex", gap: "0.4rem", marginBottom: "1rem" }}>
        {(["create", "delete", "rename"] as const).map(t => (
          <Pill key={t} val={t.toUpperCase()} active={tab === t}
            onClick={() => setTab(t)} color={C.command} />
        ))}
      </div>

      {tab === "create" && (
        <Grid cols="1fr 1fr">
          <Field label="Flight number"><Inp value={flightNum} onChange={e => setFlightNum(e.target.value)} /></Field>
          <Field label="Airport ID"><Inp type="number" value={airportId} onChange={e => setAirportId(e.target.value)} /></Field>
        </Grid>
      )}
      {tab === "delete" && (
        <Grid cols="1fr">
          <Field label="Flight ID to delete"><Inp type="number" value={delFlightId} onChange={e => setDelFlightId(e.target.value)} /></Field>
        </Grid>
      )}
      {tab === "rename" && (
        <Grid cols="1fr 1fr">
          <Field label="Flight ID"><Inp type="number" value={renameId} onChange={e => setRenameId(e.target.value)} /></Field>
          <Field label="New flight number"><Inp value={renameTo} onChange={e => setRenameTo(e.target.value)} /></Field>
        </Grid>
      )}

      <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.75rem" }}>
        <Btn onClick={execute} disabled={loading} color={C.command}>
          {loading ? "Executing…" : "▶ Execute Command"}
        </Btn>
        {history?.canUndo && (
          <Btn onClick={undo} disabled={loading} color={C.warn}>
            ↩ Undo: {history.nextUndo ?? "last"}
          </Btn>
        )}
      </div>

      {error && <p style={{ color: C.error, fontFamily: mono, fontSize: "0.82rem" }}>{error}</p>}

      {lastResp && (
        <ResultBox color={lastResp.result.success ? C.command : C.error}>
          <div style={{ color: lastResp.result.success ? C.command : C.error, marginBottom: "0.4rem" }}>
            {lastResp.result.success ? "✓" : "✗"} {lastResp.result.message}
          </div>
          <div style={{ color: C.muted, fontSize: "0.76rem" }}>
            Can undo: {lastResp.canUndo ? "yes" : "no"}
            {lastResp.nextUndo && ` | Next: "${lastResp.nextUndo}"`}
          </div>
        </ResultBox>
      )}

      {history && history.log.length > 0 && (
        <ResultBox color={C.command}>
          <div style={{ color: C.command, fontWeight: 700, marginBottom: "0.5rem" }}>Command History</div>
          {history.log.slice(0, 8).map((e, i) => (
            <div key={i} style={{
              display: "flex", justifyContent: "space-between", padding: "0.3rem 0",
              borderBottom: `1px solid ${C.border}`, alignItems: "center",
            }}>
              <span>
                {e.isUndo ? <span style={{ color: C.warn }}>↩ UNDO </span> : null}
                <span style={{ color: e.success ? C.txt : C.error, fontSize: "0.8rem" }}>{e.description}</span>
              </span>
              <Tag label={e.commandName} color={e.isUndo ? C.warn : C.command} />
            </div>
          ))}
        </ResultBox>
      )}
    </PatternCard>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 4. MEMENTO
// ─────────────────────────────────────────────────────────────────────────────

const MementoSection = () => {
  const [config, setConfig] = useState<AirportConfigSnapshot | null>(null);
  const [snapshots, setSnapshots] = useState<AirportConfigSnapshot[]>([]);
  const [snapshotLabel, setSnapshotLabel] = useState("My snapshot");
  const [editRunways, setEditRunways] = useState("");
  const [editMax, setEditMax] = useState("");
  const [editSecurity, setEditSecurity] = useState("");
  const [editNotes, setEditNotes] = useState("");
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const [c, h] = await Promise.all([getCurrentConfig(), getConfigHistory()]);
      setConfig(c); setSnapshots(h);
    } catch {}
  }, []);

  useEffect(() => { load(); }, [load]);

  const applyEdit = async () => {
    setError(null); setMsg(null); setLoading(true);
    try {
      const updated = await updateConfig({
        activeRunways: editRunways ? +editRunways : undefined,
        maxDailyFlights: editMax ? +editMax : undefined,
        securityLevel: editSecurity || undefined,
        notes: editNotes || undefined,
      });
      setConfig(updated); setMsg("Configuration updated.");
    } catch { setError("Update failed."); }
    finally { setLoading(false); }
  };

  const snap = async () => {
    setLoading(true); setError(null);
    try {
      await saveSnapshot(snapshotLabel);
      setMsg(`Snapshot "${snapshotLabel}" saved.`);
      load();
    } catch { setError("Snapshot failed."); }
    finally { setLoading(false); }
  };

  const restore = async (id: string) => {
    setLoading(true); setError(null);
    try {
      const r = await restoreSnapshot(id);
      setConfig(r.config); setMsg(r.message); load();
    } catch { setError("Restore failed."); }
    finally { setLoading(false); }
  };

  const undo = async () => {
    setLoading(true); setError(null);
    try {
      const r = await undoConfig();
      setConfig(r.config); setMsg(r.message); load();
    } catch { setError("Undo failed."); }
    finally { setLoading(false); }
  };

  return (
    <PatternCard title="Airport Configuration History" badge="Memento" color={C.memento}
      description="The Caretaker saves named Memento snapshots of the AirportConfiguration Originator. Restore any snapshot to roll back to that exact state without exposing internal fields.">

      {config && (
        <div style={{
          background: C.surface, borderRadius: 10, padding: "1rem",
          border: `1px solid ${C.memento}33`, marginBottom: "1rem",
          display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(140px, 1fr))", gap: "0.5rem",
        }}>
          {[
            ["Terminal Layout", config.terminalLayout],
            ["Active Runways", String(config.activeRunways)],
            ["Max Daily Flights", String(config.maxDailyFlights)],
            ["International", config.isInternationalEnabled ? "Yes" : "No"],
            ["Security Level", config.securityLevel],
            ["Active Gates", config.activeGates.join(", ")],
          ].map(([k, v]) => (
            <div key={k}>
              <div style={{ color: C.muted, fontSize: "0.7rem", fontFamily: mono }}>{k}</div>
              <div style={{ color: C.txt, fontSize: "0.85rem", fontFamily: mono, fontWeight: 700 }}>{v}</div>
            </div>
          ))}
          {config.notes && <div style={{ gridColumn: "1/-1", color: C.muted, fontSize: "0.75rem" }}>Notes: {config.notes}</div>}
        </div>
      )}

      <Grid cols="repeat(auto-fit, minmax(140px, 1fr))">
        <Field label="Active runways"><Inp type="number" placeholder={config ? String(config.activeRunways) : "2"} value={editRunways} onChange={e => setEditRunways(e.target.value)} /></Field>
        <Field label="Max daily flights"><Inp type="number" placeholder={config ? String(config.maxDailyFlights) : "120"} value={editMax} onChange={e => setEditMax(e.target.value)} /></Field>
        <Field label="Security level">
          <Sel value={editSecurity || config?.securityLevel || "Standard"} onChange={e => setEditSecurity(e.target.value)}>
            {["Standard", "Elevated", "High", "Critical"].map(v => <option key={v}>{v}</option>)}
          </Sel>
        </Field>
        <Field label="Notes"><Inp value={editNotes} onChange={e => setEditNotes(e.target.value)} placeholder="Optional notes" /></Field>
      </Grid>

      <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap", marginTop: "0.75rem" }}>
        <Btn onClick={applyEdit} disabled={loading} color={C.memento}>✎ Apply Changes</Btn>
        <div style={{ display: "flex", gap: "0.4rem", alignItems: "center" }}>
          <Inp value={snapshotLabel} onChange={e => setSnapshotLabel(e.target.value)}
            placeholder="Snapshot label" style={{ width: 160 }} />
          <Btn onClick={snap} disabled={loading} color={C.memento}>💾 Save Snapshot</Btn>
        </div>
        <Btn onClick={undo} disabled={loading || snapshots.length < 2} color={C.warn}>↩ Undo</Btn>
      </div>

      {error && <p style={{ color: C.error, fontFamily: mono, fontSize: "0.82rem" }}>{error}</p>}
      {msg   && <p style={{ color: C.success, fontFamily: mono, fontSize: "0.82rem" }}>{msg}</p>}

      {snapshots.length > 0 && (
        <ResultBox color={C.memento}>
          <div style={{ color: C.memento, fontWeight: 700, marginBottom: "0.5rem" }}>Snapshot History</div>
          {snapshots.map(s => (
            <div key={s.mementoId} style={{
              display: "flex", justifyContent: "space-between", alignItems: "center",
              padding: "0.35rem 0", borderBottom: `1px solid ${C.border}`,
            }}>
              <div>
                <span style={{ color: C.txt, fontSize: "0.82rem" }}>{s.label}</span>
                <span style={{ color: C.muted, fontSize: "0.72rem", marginLeft: "0.75rem" }}>
                  {new Date(s.createdAtUtc).toLocaleTimeString()} — RW:{s.activeRunways} Sec:{s.securityLevel}
                </span>
              </div>
              <Btn onClick={() => restore(s.mementoId)} color={C.memento} small>Restore</Btn>
            </div>
          ))}
        </ResultBox>
      )}
    </PatternCard>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// 5. ITERATOR
// ─────────────────────────────────────────────────────────────────────────────

const IteratorSection = () => {
  const [mode, setMode] = useState<"flights" | "facilities">("flights");
  const [facilityTypes, setFacilityTypes] = useState<string[]>([]);
  const [facilityType, setFacilityType] = useState<string>("");
  const [airportCode, setAirportCode] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize] = useState(5);
  const [flightResult, setFlightResult] = useState<IteratorPageResult<FlightIteratorItem> | null>(null);
  const [facilityResult, setFacilityResult] = useState<IteratorPageResult<AirportFacility> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { getFacilityTypes().then(setFacilityTypes).catch(() => {}); }, []);

  const fetch = useCallback(async (p: number) => {
    setError(null); setLoading(true);
    try {
      if (mode === "flights") {
        setFlightResult(await iterateFlights(p, pageSize, airportCode || undefined));
        setFacilityResult(null);
      } else {
        setFacilityResult(await iterateFacilities(p, pageSize, facilityType || undefined));
        setFlightResult(null);
      }
    } catch { setError("Iterator call failed."); }
    finally { setLoading(false); }
  }, [mode, pageSize, airportCode, facilityType]);

  useEffect(() => { fetch(1); setPage(1); }, [mode, airportCode, facilityType]);

  const go = (p: number) => { setPage(p); fetch(p); };

  const total = mode === "flights" ? flightResult?.totalPages ?? 1 : facilityResult?.totalPages ?? 1;
  const count = mode === "flights" ? flightResult?.totalCount ?? 0 : facilityResult?.totalCount ?? 0;
  const filterDesc = mode === "flights" ? flightResult?.filterDescription : facilityResult?.filterDescription;

  return (
    <PatternCard title="Collection Traversal" badge="Iterator" color={C.iterator}
      description="The FlightIterator and FacilityIterator provide sequential access to collections without exposing the underlying list. Filtering creates a FilteredFlightIterator that wraps the base collection.">

      <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1rem" }}>
        <Pill val="Flights" active={mode === "flights"} onClick={() => setMode("flights")} color={C.iterator} />
        <Pill val="Facilities" active={mode === "facilities"} onClick={() => setMode("facilities")} color={C.iterator} />
      </div>

      {mode === "flights" && (
        <Field label="Filter by airport code (optional)">
          <Inp value={airportCode} onChange={e => setAirportCode(e.target.value.toUpperCase())}
            placeholder="e.g. OTP, JFK" style={{ maxWidth: 180 }} />
        </Field>
      )}

      {mode === "facilities" && (
        <Field label="Filter by type">
          <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
            <Pill val="All" active={!facilityType} onClick={() => setFacilityType("")} color={C.iterator} />
            {facilityTypes.map(t => (
              <Pill key={t} val={t} active={facilityType === t}
                onClick={() => setFacilityType(t)} color={C.iterator} />
            ))}
          </div>
        </Field>
      )}

      {error && <p style={{ color: C.error, fontFamily: mono, fontSize: "0.82rem" }}>{error}</p>}

      {filterDesc && (
        <div style={{ color: C.muted, fontSize: "0.76rem", fontFamily: mono, margin: "0.5rem 0" }}>
          Iterator: {filterDesc} — {count} total items
        </div>
      )}

      {loading && <div style={{ color: C.iterator, fontFamily: mono, fontSize: "0.82rem" }}>Iterating…</div>}

      {/* Flight items */}
      {flightResult && flightResult.items.length > 0 && (
        <ResultBox color={C.iterator}>
          {flightResult.items.map((f, i) => (
            <div key={f.id} style={{
              display: "flex", gap: "1rem", padding: "0.4rem 0",
              borderBottom: `1px solid ${C.border}`, alignItems: "center",
            }}>
              <span style={{ color: C.muted, fontSize: "0.72rem", minWidth: 20 }}>
                {(flightResult.page - 1) * flightResult.pageSize + i + 1}
              </span>
              <span style={{ color: C.iterator, fontWeight: 700, minWidth: 70 }}>{f.flightNumber}</span>
              <span style={{ color: C.txt, fontSize: "0.8rem" }}>{f.airportName}</span>
              <Tag label={f.airportCode} color={C.iterator} />
              <span style={{ color: C.muted, fontSize: "0.72rem", marginLeft: "auto" }}>
                {new Date(f.createdAt).toLocaleDateString()}
              </span>
            </div>
          ))}
        </ResultBox>
      )}

      {/* Facility items */}
      {facilityResult && facilityResult.items.length > 0 && (
        <ResultBox color={C.iterator}>
          {facilityResult.items.map((f, i) => (
            <div key={f.facilityId} style={{
              display: "flex", gap: "1rem", padding: "0.4rem 0",
              borderBottom: `1px solid ${C.border}`, alignItems: "center",
            }}>
              <span style={{ color: C.muted, fontSize: "0.72rem", minWidth: 20 }}>
                {(facilityResult.page - 1) * facilityResult.pageSize + i + 1}
              </span>
              <span style={{ color: C.iterator, fontWeight: 700, minWidth: 90 }}>{f.name}</span>
              <Tag label={f.facilityType} color={C.iterator} />
              <span style={{ color: C.muted, fontSize: "0.78rem" }}>{f.zone}</span>
              <Tag label={f.isOperational ? "Operational" : "Offline"} color={f.isOperational ? C.success : C.error} />
              {f.notes && <span style={{ color: C.muted, fontSize: "0.72rem", marginLeft: "auto" }}>{f.notes}</span>}
            </div>
          ))}
        </ResultBox>
      )}

      {/* Pagination */}
      {total > 1 && (
        <div style={{ display: "flex", gap: "0.4rem", marginTop: "0.75rem", alignItems: "center" }}>
          <Btn onClick={() => go(page - 1)} disabled={page <= 1 || loading} color={C.iterator} small>‹ Prev</Btn>
          <span style={{ color: C.muted, fontFamily: mono, fontSize: "0.8rem" }}>
            Page {page} / {total}
          </span>
          <Btn onClick={() => go(page + 1)} disabled={page >= total || loading} color={C.iterator} small>Next ›</Btn>
        </div>
      )}
    </PatternCard>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Root export
// ─────────────────────────────────────────────────────────────────────────────

const PATTERNS = [
  { color: C.strategy, key: "strategy", label: "Strategy",  desc: "Interchangeable scheduling algorithms" },
  { color: C.observer, key: "observer", label: "Observer",  desc: "Event-driven status propagation" },
  { color: C.command,  key: "command",  label: "Command",   desc: "Encapsulated actions with undo" },
  { color: C.memento,  key: "memento",  label: "Memento",   desc: "Configuration snapshots & rollback" },
  { color: C.iterator, key: "iterator", label: "Iterator",  desc: "Transparent collection traversal" },
];

const headerStyle: CSSProperties = {
  fontFamily: mono,
  background: C.surface,
  border: `1px solid ${C.border}`,
  borderRadius: 12,
  padding: "1.25rem 1.5rem",
  marginBottom: "2rem",
  display: "flex",
  flexWrap: "wrap",
  gap: "1rem",
  alignItems: "center",
};

const BehavioralPatternsDemoSection = () => (
  <section style={{ marginTop: "3rem", textAlign: "left" }}>
    <h2 style={{ fontFamily: mono, color: C.txt, marginBottom: "0.25rem" }}>
      Behavioral Design Patterns
    </h2>
    <p style={{ color: C.muted, fontSize: "0.85rem", marginBottom: "1.5rem" }}>
      Live demos backed by real PostgreSQL data — Strategy, Observer, Command, Memento, Iterator.
    </p>

    {/* Pattern overview bar */}
    <div style={headerStyle}>
      {PATTERNS.map(p => (
        <div key={p.key} style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          <span style={{ width: 8, height: 8, borderRadius: "50%", background: p.color, display: "inline-block" }} />
          <Tag label={p.label} color={p.color} />
          <span style={{ color: C.muted, fontSize: "0.75rem" }}>{p.desc}</span>
        </div>
      ))}
    </div>

    <StrategySection />
    <ObserverSection />
    <CommandSection />
    <MementoSection />
    <IteratorSection />
  </section>
);

export default BehavioralPatternsDemoSection;
