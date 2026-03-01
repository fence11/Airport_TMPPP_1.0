import { apiClient } from "./client";

export type TransportType = 0 | 1 | 2;

export type ReservationSummary = {
    customerName: string;
    transportName: string;
    distanceKm: number;
    price: number;
    transportDescription: string;
};

export type CreateReservationRequest = {
    transportType: TransportType;
    customerName: string;
    distanceKm: number;
};

export const createReservation = async (
    request: CreateReservationRequest
): Promise<ReservationSummary> => {
    const response = await apiClient.post<ReservationSummary>("/api/Reservations", request);
    console.log("[Reservations] Server response:", response.data);
    return response.data;
};
