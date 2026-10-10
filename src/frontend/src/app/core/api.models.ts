export const supportedCrops = ['Paddy', 'Maize', 'Green Gram', 'Cowpea', 'Groundnut', 'Chilli'] as const;

export const validationStatuses = ['Pending Validation', 'Validated', 'Needs Review'] as const;

export type UserRole = 'Registered User' | 'Agricultural Officer' | 'Administrator';

export interface ApiMessage {
  message?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  userId: number;
  name: string;
  email: string;
  role: UserRole;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

export interface RegisterResponse {
  userId: number;
  name: string;
  email: string;
  role: UserRole;
  createdAt: string;
}

export interface CurrentUserResponse {
  id: number;
  email: string;
  role: UserRole;
}

export interface CreateRecommendationRequest {
  district: string;
  soilType?: string | null;
}

export interface RecommendationResponse {
  recommendationId: number;
  forecastRunId: string;
  userId: number | null;
  soilType: string | null;
  createdAt: string;
  forecast: RecommendationForecastResponse;
  crops: RecommendationCropResponse[];
}

export interface RecommendationForecastResponse {
  forecastRunId: string;
  forecastDate: string;
  targetStartDate: string;
  targetEndDate: string;
  modelVersion: string | null;
  createdAt: string;
  days: RecommendationForecastDayResponse[];
}

export interface RecommendationForecastDayResponse {
  targetDate: string;
  rainfall: number;
  temperature: number;
  humidity: number;
}

export interface RecommendationCropResponse {
  cropName: string;
  evaluationStatus: string;
  rainfallScore: number | null;
  temperatureScore: number | null;
  humidityScore: number | null;
  soilScore: number | null;
  overallScore: number | null;
  suitabilityCategory: string | null;
  rank: number | null;
  unavailableFactors: string[];
  factors: RecommendationFactorResponse[];
  evidenceCoverage: EvidenceCoverage;
  climateRisks: string[];
  explanation: string;
}

export interface EvidenceCoverage {
  evaluatedFactorCount: number;
  totalFactorCount: number;
  evaluatedConfiguredWeightTotal: number;
}

export interface RecommendationFactorResponse {
  factor: string;
  isEvaluable: boolean;
  score: number | null;
  aggregatedValue: number | null;
  configuredWeight: number;
  effectiveWeight: number | null;
  explanation: string;
}

export interface RecommendationReviewSummaryResponse {
  recommendationId: number;
  forecastRunId: string;
  userId: number | null;
  soilType: string | null;
  createdAt: string;
  latestValidationStatus: string | null;
  latestValidationCreatedAt: string | null;
}

export interface RecommendationReviewResponse {
  recommendationId: number;
  forecastRunId: string;
  userId: number | null;
  soilType: string | null;
  createdAt: string;
  evidenceSnapshotJson: string;
  crops: RecommendationReviewCropResponse[];
  validationHistory: RecommendationValidationResponse[];
}

export interface RecommendationReviewCropResponse {
  cropId: number;
  cropName: string;
  rainfallScore: number | null;
  temperatureScore: number | null;
  humidityScore: number | null;
  soilScore: number | null;
  overallScore: number | null;
  suitabilityCategory: string | null;
  rank: number | null;
  evaluationStatus: string;
  explanation: string;
  evidenceSnapshotJson: string;
}

export interface SubmitRecommendationValidationRequest {
  status: string;
  comment?: string | null;
}

export interface RecommendationValidationResponse {
  id: number;
  recommendationId: number;
  officerUserId: number;
  status: string;
  comment: string | null;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface RecommendationHistorySummaryResponse {
  recommendationId: number;
  forecastRunId: string;
  soilType: string | null;
  createdAt: string;
  cropResultCount: number;
  topCropName: string | null;
  topSuitabilityCategory: string | null;
  topOverallScore: number | null;
}

export interface RecommendationHistoryDetailResponse {
  recommendationId: number;
  forecastRunId: string;
  soilType: string | null;
  createdAt: string;
  forecast: RecommendationHistoryForecastResponse | null;
  crops: RecommendationHistoryCropResponse[];
  provenance: string[];
  limitations: string[];
}

export interface RecommendationHistoryForecastResponse {
  forecastRunId: string;
  forecastDate: string | null;
  targetStartDate: string | null;
  targetEndDate: string | null;
  modelVersion: string | null;
  days: RecommendationHistoryForecastDayResponse[];
}

export interface RecommendationHistoryForecastDayResponse {
  targetDate: string;
  rainfall: number | null;
  temperature: number | null;
  humidity: number | null;
}

export interface RecommendationHistoryCropResponse {
  cropName: string;
  evaluationStatus: string;
  rainfallScore: number | null;
  temperatureScore: number | null;
  humidityScore: number | null;
  soilScore: number | null;
  overallScore: number | null;
  suitabilityCategory: string | null;
  rank: number | null;
  unavailableFactors: string[];
  factors: RecommendationHistoryFactorResponse[];
  climateRisks: string[];
  explanation: string;
}

export interface RecommendationHistoryFactorResponse {
  factor: string;
  isEvaluable: boolean;
  score: number | null;
  aggregatedValue: number | null;
  configuredWeight: number | null;
  effectiveWeight: number | null;
  explanation: string;
}

export interface CropResponse {
  id: number;
  name: string;
  isActive: boolean;
}

export interface CropRequirementResponse {
  id: number;
  cropId: number;
  cropName: string;
  variableType: string;
  minimumValue: number;
  maximumValue: number;
  acceptableMinimumValue: number | null;
  acceptableMaximumValue: number | null;
  unit: string;
  timeBasis: string | null;
  isCompatibleWithSevenDayForecast: boolean;
  isActive: boolean;
}

export interface SoilCompatibilityResponse {
  id: number;
  cropId: number;
  cropName: string;
  soilType: string;
  compatibilityScore: number;
  isActive: boolean;
}

export interface SuitabilityConfigurationResponse {
  id: number;
  configurationType: string;
  configurationKey: string;
  value: number;
  isActive: boolean;
  updatedAt: string;
  updatedByUserId: number | null;
}

export interface AdminUserResponse {
  id: number;
  name: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface AdminRecommendationSummaryResponse {
  id: number;
  userId: number | null;
  userEmail: string | null;
  forecastRunId: string;
  soilType: string | null;
  createdAt: string;
  cropResultCount: number;
}

export interface AdminValidationResponse {
  id: number;
  recommendationId: number;
  officerUserId: number;
  officerEmail: string;
  status: string;
  comment: string | null;
  createdAt: string;
}

export type AdminResource =
  | 'users'
  | 'crops'
  | 'crop-requirements'
  | 'soil-compatibility'
  | 'suitability-config'
  | 'recommendations'
  | 'validations';
