import { apiClient } from "./client";

// ── Strategy ──────────────────────────────────────────────────────────────

export type ScheduledFlight = {
  flightId: number;
  flightNumber: string;
  runwayId: number;
  scheduledTime: string;
  strategyUsed: string;
  reason: string;
};

export type ScheduleResult = {
  strategy: string;
  description: string;
  windowStart: string;
  totalFlights: number;
  totalRunways: number;
  schedule: ScheduledFlight[];
};

export type StrategyOption = {
  key: string;
  name: string;
  description: string;
};

export const runSchedulingStrategy = async (
  strategy: string,
  runwayCount: number
): Promise<ScheduleResult> => {
  const res = await apiClient.get<ScheduleResult>(
    "/api/BehavioralPatterns/strategy/schedule",
    { params: { strategy, runwayCount } }
  );
  return res.data;
};

export const getAvailableStrategies = async (): Promise<StrategyOption[]> => {
  const res = await apiClient.get<StrategyOption[]>(
    "/api/BehavioralPatterns/strategy/strategies"
  );
  return res.data;
};

// ── Observer ──────────────────────────────────────────────────────────────

export type FlightStatusEvent = {
  flightId: number;
  flightNumber: string;
  oldStatus: string;
  newStatus: string;
  gateCode: string | null;
  reason: string | null;
  changedAtUtc: string;
};

export type ObserverLog = {
  observerId: string;
  observerType: string;
  recentLog: string[];
};

export type ObserverLogsResponse = {
  eventHistory: FlightStatusEvent[];
  observers: ObserverLog[];
};

export type StatusChangeResponse = {
  event: FlightStatusEvent;
  observerNotifications: Record<string, { observerType: string; log: string[] }>;
};

export const changeFlightStatus = async (
  flightNumber: string,
  newStatus: string,
  oldStatus?: string,
  gateCode?: string,
  reason?: string
): Promise<StatusChangeResponse> => {
  const res = await apiClient.post<StatusChangeResponse>(
    "/api/BehavioralPatterns/observer/status-change",
    { flightNumber, newStatus, oldStatus, gateCode, reason }
  );
  return res.data;
};

export const getObserverLogs = async (): Promise<ObserverLogsResponse> => {
  const res = await apiClient.get<ObserverLogsResponse>(
    "/api/BehavioralPatterns/observer/logs"
  );
  return res.data;
};

export const getFlightStatuses = async (): Promise<string[]> => {
  const res = await apiClient.get<string[]>(
    "/api/BehavioralPatterns/observer/statuses"
  );
  return res.data;
};

// ── Command ───────────────────────────────────────────────────────────────

export type CommandResult = {
  success: boolean;
  message: string;
  payload?: unknown;
};

export type CommandResponse = {
  result: CommandResult;
  canUndo: boolean;
  nextUndo: string | null;
};

export type CommandLogEntry = {
  commandId: string;
  commandName: string;
  description: string;
  issuedAtUtc: string;
  success: boolean;
  message: string;
  isUndo: boolean;
};

export type CommandHistoryResponse = {
  canUndo: boolean;
  nextUndo: string | null;
  log: CommandLogEntry[];
};

export const commandCreateFlight = async (
  flightNumber: string,
  airportId: number
): Promise<CommandResponse> => {
  const res = await apiClient.post<CommandResponse>(
    "/api/BehavioralPatterns/command/create-flight",
    { flightNumber, airportId }
  );
  return res.data;
};

export const commandDeleteFlight = async (
  flightId: number
): Promise<CommandResponse> => {
  const res = await apiClient.post<CommandResponse>(
    `/api/BehavioralPatterns/command/delete-flight/${flightId}`
  );
  return res.data;
};

export const commandRenameFlight = async (
  flightId: number,
  newFlightNumber: string
): Promise<CommandResponse> => {
  const res = await apiClient.post<CommandResponse>(
    "/api/BehavioralPatterns/command/rename-flight",
    { flightId, newFlightNumber }
  );
  return res.data;
};

export const undoLastCommand = async (): Promise<CommandResponse> => {
  const res = await apiClient.post<CommandResponse>(
    "/api/BehavioralPatterns/command/undo"
  );
  return res.data;
};

export const getCommandHistory = async (): Promise<CommandHistoryResponse> => {
  const res = await apiClient.get<CommandHistoryResponse>(
    "/api/BehavioralPatterns/command/history"
  );
  return res.data;
};

// ── Memento ───────────────────────────────────────────────────────────────

export type AirportConfigSnapshot = {
  mementoId: string;
  label: string;
  createdAtUtc: string;
  terminalLayout: string;
  activeRunways: number;
  maxDailyFlights: number;
  isInternationalEnabled: boolean;
  activeGates: string[];
  securityLevel: string;
  notes: string;
};

export type UpdateConfigRequest = {
  terminalLayout?: string;
  activeRunways?: number;
  maxDailyFlights?: number;
  isInternationalEnabled?: boolean;
  activeGates?: string[];
  securityLevel?: string;
  notes?: string;
};

export const getCurrentConfig = async (): Promise<AirportConfigSnapshot> => {
  const res = await apiClient.get<AirportConfigSnapshot>(
    "/api/BehavioralPatterns/memento/config"
  );
  return res.data;
};

export const updateConfig = async (
  req: UpdateConfigRequest
): Promise<AirportConfigSnapshot> => {
  const res = await apiClient.patch<AirportConfigSnapshot>(
    "/api/BehavioralPatterns/memento/config",
    req
  );
  return res.data;
};

export const saveSnapshot = async (label: string): Promise<{ mementoId: string; message: string }> => {
  const res = await apiClient.post<{ mementoId: string; message: string }>(
    "/api/BehavioralPatterns/memento/snapshot",
    { label }
  );
  return res.data;
};

export const restoreSnapshot = async (
  mementoId: string
): Promise<{ message: string; config: AirportConfigSnapshot }> => {
  const res = await apiClient.post<{ message: string; config: AirportConfigSnapshot }>(
    `/api/BehavioralPatterns/memento/restore/${mementoId}`
  );
  return res.data;
};

export const undoConfig = async (): Promise<{ message: string; config: AirportConfigSnapshot }> => {
  const res = await apiClient.post<{ message: string; config: AirportConfigSnapshot }>(
    "/api/BehavioralPatterns/memento/undo"
  );
  return res.data;
};

export const getConfigHistory = async (): Promise<AirportConfigSnapshot[]> => {
  const res = await apiClient.get<AirportConfigSnapshot[]>(
    "/api/BehavioralPatterns/memento/history"
  );
  return res.data;
};

// ── Iterator ──────────────────────────────────────────────────────────────

export type FlightIteratorItem = {
  id: number;
  flightNumber: string;
  airportId: number;
  airportName: string;
  airportCode: string;
  createdAt: string;
};

export type AirportFacility = {
  facilityId: string;
  name: string;
  facilityType: string;
  zone: string;
  isOperational: boolean;
  notes: string | null;
};

export type IteratorPageResult<T> = {
  iteratorType: string;
  filterDescription: string;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: T[];
};

export const iterateFlights = async (
  page: number,
  pageSize: number,
  airportCode?: string
): Promise<IteratorPageResult<FlightIteratorItem>> => {
  const res = await apiClient.get<IteratorPageResult<FlightIteratorItem>>(
    "/api/BehavioralPatterns/iterator/flights",
    { params: { page, pageSize, airportCode } }
  );
  return res.data;
};

export const iterateFacilities = async (
  page: number,
  pageSize: number,
  type?: string
): Promise<IteratorPageResult<AirportFacility>> => {
  const res = await apiClient.get<IteratorPageResult<AirportFacility>>(
    "/api/BehavioralPatterns/iterator/facilities",
    { params: { page, pageSize, type } }
  );
  return res.data;
};

export const getFacilityTypes = async (): Promise<string[]> => {
  const res = await apiClient.get<string[]>(
    "/api/BehavioralPatterns/iterator/facility-types"
  );
  return res.data;
};
