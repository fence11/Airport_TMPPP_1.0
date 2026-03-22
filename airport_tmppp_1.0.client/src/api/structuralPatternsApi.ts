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
