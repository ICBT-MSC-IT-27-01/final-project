import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  ApiMessage,
  AdminResource,
  CreateRecommendationRequest,
  CurrentUserResponse,
  LoginRequest,
  LoginResponse,
  PagedResponse,
  RecommendationHistoryDetailResponse,
  RecommendationHistorySummaryResponse,
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

  listRecommendationHistory(page = 1, pageSize = 20): Observable<PagedResponse<RecommendationHistorySummaryResponse>> {
    return this.http
      .get<PagedResponse<RecommendationHistorySummaryResponse>>(`${this.baseUrl}/recommendations/history`, {
        params: { page, pageSize },
      })
      .pipe(catchError(toApiError));
  }

  getRecommendationHistoryDetail(recommendationId: number): Observable<RecommendationHistoryDetailResponse> {
    return this.http
      .get<RecommendationHistoryDetailResponse>(`${this.baseUrl}/recommendations/${recommendationId}`)
      .pipe(catchError(toApiError));
  }

  listAdminResources<T>(resource: AdminResource, page = 1, pageSize = 20): Observable<PagedResponse<T>> {
    return this.http
      .get<PagedResponse<T>>(`${this.baseUrl}/admin/${resource}`, { params: { page, pageSize } })
      .pipe(catchError(toApiError));
  }

  createAdminResource<TRequest, TResponse>(resource: AdminResource, request: TRequest): Observable<TResponse> {
    return this.http.post<TResponse>(`${this.baseUrl}/admin/${resource}`, request).pipe(catchError(toApiError));
  }

  updateAdminResource<TRequest, TResponse>(resource: AdminResource, id: number, request: TRequest): Observable<TResponse> {
    return this.http.put<TResponse>(`${this.baseUrl}/admin/${resource}/${id}`, request).pipe(catchError(toApiError));
  }

  setAdminResourceActiveStatus<TResponse>(
    resource: Exclude<AdminResource, 'recommendations' | 'validations'>,
    id: number,
    isActive: boolean,
  ): Observable<TResponse> {
    return this.http
      .patch<TResponse>(`${this.baseUrl}/admin/${resource}/${id}/active-status`, { isActive })
      .pipe(catchError(toApiError));
  }

  updateAdminUserRole(userId: number, role: string): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/admin/users/${userId}/role`, { role }).pipe(catchError(toApiError));
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
