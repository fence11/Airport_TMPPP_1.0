import { apiClient } from "./client";

// ── Adapter ──────────────────────────────────────────────────────────────────

export type Gateway = "paypal" | "stripe" | "googlepay";

export type AdapterPaymentRequest = {
  gateway: Gateway;
  customerName: string;
  amount: number;
  currency: string;
  flightNumber: string;
};

export type AdapterPaymentResponse = {
  gateway: string;
  success: boolean;
  transactionId: string;
  message: string;
};

export const adapterPay = async (
  req: AdapterPaymentRequest
): Promise<AdapterPaymentResponse> => {
  const res = await apiClient.post<AdapterPaymentResponse>(
    "/api/StructuralPatterns/adapter/pay",
    req
  );
  return res.data;
};

// ── Composite ─────────────────────────────────────────────────────────────────

export type MenuItemDto = {
  name: string;
  description: string;
  totalPrice: number;
  category: string | null;
  children: MenuItemDto[] | null;
};

export const getRestaurantMenu = async (): Promise<MenuItemDto> => {
  const res = await apiClient.get<MenuItemDto>(
    "/api/StructuralPatterns/composite/menu"
  );
  return res.data;
};

export const filterMenu = async (
  minPrice: number,
  maxPrice: number
): Promise<{ count: number; items: MenuItemDto[] }> => {
  const res = await apiClient.get<{ count: number; items: MenuItemDto[] }>(
    "/api/StructuralPatterns/composite/menu/filter",
    { params: { minPrice, maxPrice } }
  );
  return res.data;
};

// ── Façade ────────────────────────────────────────────────────────────────────

export type RoomInfo = {
  typeName: string;
  maxGuests: number;
  pricePerNight: number;
  description: string;
};

export type BookRoomRequest = {
  guestName: string;
  guestEmail: string;
  guestPhone?: string;
  roomTypeCode: string;
  guestCount: number;
  checkIn: string;
  checkOut: string;
  paymentMethod: string;
};

export type BookingConfirmation = {
  bookingReference: string;
  guestName: string;
  roomType: string;
  checkIn: string;
  checkOut: string;
  totalPrice: number;
  transactionId: string;
};

export const searchRooms = async (
  checkIn: string,
  checkOut: string,
  guestCount: number
): Promise<RoomInfo[]> => {
  const res = await apiClient.get<RoomInfo[]>(
    "/api/StructuralPatterns/facade/rooms",
    { params: { checkIn, checkOut, guestCount } }
  );
  return res.data;
};

export const bookRoom = async (
  req: BookRoomRequest
): Promise<BookingConfirmation> => {
  const res = await apiClient.post<BookingConfirmation>(
    "/api/StructuralPatterns/facade/book-room",
    req
  );
  return res.data;
};

export const cancelBooking = async (bookingReference: string): Promise<void> => {
  await apiClient.delete(
    `/api/StructuralPatterns/facade/cancel/${bookingReference}`
  );
};

// ── Flyweight ────────────────────────────────────────────────────────────────

export type FlyweightUsage = {
  contextId: string;
  flightCode: string;
  resourceType: string;
  zone: string;
  details: string;
};

export type FlyweightDemoResponse = {
  sharedObjects: number;
  totalAssignments: number;
  usages: FlyweightUsage[];
};

export const getFlyweightResources = async (): Promise<FlyweightDemoResponse> => {
  const res = await apiClient.get<FlyweightDemoResponse>(
    "/api/StructuralPatterns/flyweight/resources"
  );
  return res.data;
};

// ── Decorator ────────────────────────────────────────────────────────────────

export type DecoratorBookingRequest = {
  flightNumber: string;
  basePrice: number;
  addPriorityBoarding: boolean;
  addLoungeAccess: boolean;
};

export type DecoratorBookingResponse = {
  description: string;
  totalPrice: number;
};

export const decorateBooking = async (
  req: DecoratorBookingRequest
): Promise<DecoratorBookingResponse> => {
  const res = await apiClient.post<DecoratorBookingResponse>(
    "/api/StructuralPatterns/decorator/booking",
    req
  );
  return res.data;
};

// ── Bridge ───────────────────────────────────────────────────────────────────

export type AirportKind = "international" | "domestic" | "cargo";
export type OperationKind = "landing" | "security";

export type BridgeOperationResponse = {
  airportType: string;
  operation: string;
  result: string;
};

export const runBridgeOperation = async (
  airportType: AirportKind,
  operation: OperationKind,
  identifier: string
): Promise<BridgeOperationResponse> => {
  const res = await apiClient.get<BridgeOperationResponse>(
    "/api/StructuralPatterns/bridge/operations",
    { params: { airportType, operation, identifier } }
  );
  return res.data;
};

// ── Proxy ────────────────────────────────────────────────────────────────────

export type ProxySystem = "atc" | "securitydb";

export type ProxyAccessResponse = {
  system: string;
  role: string;
  result: string;
};

export const proxyAccess = async (
  system: ProxySystem,
  role: string,
  query: string
): Promise<ProxyAccessResponse> => {
  const res = await apiClient.get<ProxyAccessResponse>(
    "/api/StructuralPatterns/proxy/access",
    { params: { system, role, query } }
  );
  return res.data;
};
