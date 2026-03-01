import axios from "axios";

const API_BASE = "";

export const apiClient = axios.create({
    baseURL: API_BASE,
});

apiClient.interceptors.response.use(
    (response) => {
        console.log("[API Response]", response.config.method?.toUpperCase(), response.config.url, response.status, response.data);
        return response;
    },
    (error) => {
        console.log("[API Error]", error.config?.method?.toUpperCase(), error.config?.url, error.response?.status, error.response?.data ?? error.message);
        return Promise.reject(error);
    }
);
