import { apiClient } from "./client";

// ── Chain of Responsibility ───────────────────────────────────────────────────

export type HandlerStep = {
  handlerName: string;
  passed: boolean;
  message: string;
};

export type BookingResult = {
  success: boolean;
  bookingRef: string;
  finalMessage: string;
  chainTrace: HandlerStep[];
};

export type ChainContext = {
  flights: { id: number; flightNumber: string; airportId: number }[];
  passengers: { id: number; firstName: string; lastName: string; email: string }[];
};

export type BookingRequestDto = {
  passengerName: string;
  passengerEmail: string;
  flightId: number;
  flightNumber: string;
  baggageWeightKg: number;
  hasSpecialMeal: boolean;
  paymentMethod: string;
  ticketPrice: number;
};

export const processBooking = async (dto: BookingRequestDto): Promise<BookingResult> => {
  const res = await apiClient.post<BookingResult>("/api/AdvancedPatterns/chain/book", dto);
  return res.data;
};

export const getChainContext = async (): Promise<ChainContext> => {
  const res = await apiClient.get<ChainContext>("/api/AdvancedPatterns/chain/context");
  return res.data;
};

// ── State ─────────────────────────────────────────────────────────────────────

export type StateTransitionRecord = {
  flightId: number;
  flightNumber: string;
  from: string;
  to: string;
  actor: string;
  note: string | null;
  occurredAtUtc: string;
};

export type FlightStateContext = {
  flightId: number;
  flightNumber: string;
  currentStatus: string;
  statusLabel: string;
  statusDescription: string;
  allowedTransitions: string[];
  history: StateTransitionRecord[];
};

export type StateActionResult = {
  success: boolean;
  message: string;
  newStatus: string | null;
};

export type TransitionResponse = {
  result: StateActionResult;
  context: FlightStateContext;
};

export const getFlightState = async (flightId: number): Promise<FlightStateContext> => {
  const res = await apiClient.get<FlightStateContext>(`/api/AdvancedPatterns/state/flight/${flightId}`);
  return res.data;
};

export const getAllFlightStates = async (): Promise<FlightStateContext[]> => {
  const res = await apiClient.get<FlightStateContext[]>("/api/AdvancedPatterns/state/all");
  return res.data;
};

export const transitionFlightState = async (
  flightId: number,
  action: string,
  actor?: string,
  note?: string
): Promise<TransitionResponse> => {
  const res = await apiClient.post<TransitionResponse>(
    `/api/AdvancedPatterns/state/flight/${flightId}/transition`,
    { action, actor, note }
  );
  return res.data;
};

export const getStateActions = async (): Promise<string[]> => {
  const res = await apiClient.get<string[]>("/api/AdvancedPatterns/state/actions");
  return res.data;
};

// ── Mediator ──────────────────────────────────────────────────────────────────

export type ServiceLogEntry = {
  serviceName: string;
  operation: string;
  success: boolean;
  message: string;
  occurredAtUtc: string;
};

export type CoordinationResult = {
  success: boolean;
  operationType: string;
  summary: string;
  serviceLog: ServiceLogEntry[];
};

export type OperationType = {
  key: string;
  label: string;
  services: string[];
};

export type CoordinationRequestDto = {
  operationType: string;
  flightId: number;
  flightNumber: string;
  passengerEmail?: string;
  gateCode?: string;
  note?: string;
};

export const coordinateServices = async (dto: CoordinationRequestDto): Promise<CoordinationResult> => {
  const res = await apiClient.post<CoordinationResult>("/api/AdvancedPatterns/mediator/coordinate", dto);
  return res.data;
};

export const getMediatorLog = async (): Promise<ServiceLogEntry[]> => {
  const res = await apiClient.get<ServiceLogEntry[]>("/api/AdvancedPatterns/mediator/log");
  return res.data;
};

export const getMediatorOperationTypes = async (): Promise<OperationType[]> => {
  const res = await apiClient.get<OperationType[]>("/api/AdvancedPatterns/mediator/operation-types");
  return res.data;
};

// ── Template Method ───────────────────────────────────────────────────────────

export type OperationStep = {
  stepName: string;
  completed: boolean;
  result: string;
  executedAtUtc: string;
};

export type FlightOperationReport = {
  operationName: string;
  flightNumber: string;
  flightId: number;
  overallSuccess: boolean;
  summary: string;
  steps: OperationStep[];
};

export type TemplateOperationType = {
  key: string;
  label: string;
  description: string;
};

export const runFlightOperation = async (
  operationType: string,
  flightId: number
): Promise<FlightOperationReport> => {
  const res = await apiClient.post<FlightOperationReport>(
    "/api/AdvancedPatterns/template/run",
    { operationType, flightId }
  );
  return res.data;
};

export const getTemplateOperationTypes = async (): Promise<TemplateOperationType[]> => {
  const res = await apiClient.get<TemplateOperationType[]>(
    "/api/AdvancedPatterns/template/operation-types"
  );
  return res.data;
};

// ── Visitor ───────────────────────────────────────────────────────────────────

export type ReportRow = {
  label: string;
  value: string;
};

export type VisitorReport = {
  reportType: string;
  generatedAtUtc: string;
  summary: string | null;
  rows: ReportRow[];
};

export type ReportType = {
  key: string;
  label: string;
  description: string;
};

export const generateReport = async (type: string): Promise<VisitorReport> => {
  const res = await apiClient.get<VisitorReport>(
    "/api/AdvancedPatterns/visitor/report",
    { params: { type } }
  );
  return res.data;
};

export const getReportTypes = async (): Promise<ReportType[]> => {
  const res = await apiClient.get<ReportType[]>("/api/AdvancedPatterns/visitor/report-types");
  return res.data;
};
