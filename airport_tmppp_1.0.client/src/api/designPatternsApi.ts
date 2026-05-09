import { apiClient } from "./client";

// ===== Builder =====

export type TravelPackage = {
  demoTitle: string;
  customerName: string;
  destination: string;
  startDate: string;
  endDate: string;
  transport: string;
  accommodation: string;
  activities: string[];
  price: number;
};

export const getWeekendCityBreak = async (
  customerName: string,
  city: string
): Promise<TravelPackage> => {
  const response = await apiClient.get<TravelPackage>("/api/DesignPatterns/builder/weekend", {
    params: { customerName, city },
  });
  return response.data;
};

export const getBeachHoliday = async (
  customerName: string,
  resort: string
): Promise<TravelPackage> => {
  const response = await apiClient.get<TravelPackage>("/api/DesignPatterns/builder/beach", {
    params: { customerName, resort },
  });
  return response.data;
};

// ===== Prototype =====

export type TravelDocument = {
  title: string;
  passengerName: string;
  destination: string;
  travelDate: string;
  metadata: Record<string, string>;
  notes: string[];
};

export type PrototypeDemoResponse = {
  original: TravelDocument;
  clone: TravelDocument;
};

export const getPrototypeDemo = async (): Promise<PrototypeDemoResponse> => {
  const response = await apiClient.get<PrototypeDemoResponse>("/api/DesignPatterns/prototype");
  return response.data;
};

// ===== Singleton =====

export type DatabaseConnectionInfo = {
  connectionId: string;
  connectionString: string;
  lastUsedUtc: string;
};

export const getSingletonInfo = async (): Promise<DatabaseConnectionInfo> => {
  const response = await apiClient.get<DatabaseConnectionInfo>("/api/DesignPatterns/singleton");
  return response.data;
};

// ===== Abstract Factory =====

export type TravelerProfile = "business" | "family";

export type AbstractFactoryBundle = {
  profile: string;
  loungeName: string;
  loungeBenefits: string;
  transferType: string;
  transferDetails: string;
  insurancePlan: string;
  insuranceCoverage: string;
};

export const getAbstractFactoryBundle = async (
  profile: TravelerProfile
): Promise<AbstractFactoryBundle> => {
  const response = await apiClient.get<AbstractFactoryBundle>(
    "/api/DesignPatterns/abstract-factory/service-bundle",
    { params: { profile } }
  );
  return response.data;
};

