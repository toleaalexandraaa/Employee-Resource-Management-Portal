export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  userId: string;
  fullName: string;
  role: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}
