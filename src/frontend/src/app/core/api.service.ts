import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  ApiMessage,
  CreateRecommendationRequest,
  CurrentUserResponse,
  LoginRequest,
  LoginResponse,
  PagedResponse,
  RecommendationResponse,
  RecommendationReviewResponse,
  RecommendationReviewSummaryResponse,
  RecommendationValidationResponse,
  RegisterRequest,
  RegisterResponse,
  SubmitRecommendationValidationRequest,
} from './api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = '/api';

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/auth/login`, request).pipe(catchError(toApiError));
  }

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.baseUrl}/auth/register`, request).pipe(catchError(toApiError));
  }

  getCurrentUser(): Observable<CurrentUserResponse> {
    return this.http.get<CurrentUserResponse>(`${this.baseUrl}/auth/me`).pipe(catchError(toApiError));
  }

  createRecommendation(request: CreateRecommendationRequest): Observable<RecommendationResponse> {
    return this.http.post<RecommendationResponse>(`${this.baseUrl}/recommendations`, request).pipe(catchError(toApiError));
  }

  listPendingValidations(): Observable<RecommendationReviewSummaryResponse[]> {
    return this.http
      .get<RecommendationReviewSummaryResponse[]>(`${this.baseUrl}/validations/pending`)
      .pipe(catchError(toApiError));
  }

  getRecommendationReview(recommendationId: number): Observable<RecommendationReviewResponse> {
    return this.http
      .get<RecommendationReviewResponse>(`${this.baseUrl}/validations/recommendations/${recommendationId}`)
      .pipe(catchError(toApiError));
  }

  submitRecommendationValidation(
    recommendationId: number,
    request: SubmitRecommendationValidationRequest,
  ): Observable<RecommendationValidationResponse> {
    return this.http
      .post<RecommendationValidationResponse>(`${this.baseUrl}/validations/recommendations/${recommendationId}`, request)
      .pipe(catchError(toApiError));
  }

  listAdminResources<T>(resource: string): Observable<PagedResponse<T>> {
    return this.http.get<PagedResponse<T>>(`${this.baseUrl}/admin/${resource}`).pipe(catchError(toApiError));
  }
}

export class FrontendApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
  }
}

function toApiError(error: HttpErrorResponse) {
  const body = error.error as ApiMessage | undefined;
  const message = body?.message || 'The request could not be completed.';
  return throwError(() => new FrontendApiError(message, error.status));
}
