import axios from "axios";
import type { Flight } from "../features/flights/flightTypes";

const API_URL = "https://localhost:7281/api/Flights"

export const getFlights = async (): Promise<Flight[]> => {
    const response = await axios.get<Flight[]>(API_URL);
    return response.data;
};
