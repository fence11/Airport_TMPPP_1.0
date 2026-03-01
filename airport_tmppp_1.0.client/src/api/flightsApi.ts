import { apiClient } from "./client";
import type { Flight } from "../features/flights/flightTypes";

export const getFlights = async (): Promise<Flight[]> => {
    const response = await apiClient.get<Flight[]>("/api/Flights");
    console.log("[Flights] Server response:", response.data);
    return response.data;
};
