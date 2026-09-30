export interface UserInfo {
  id: string;
  name: string;
  email: string;
  plan: string;
}

export interface AuthResponse {
  token: string;
  user: UserInfo;
}

export interface RegisterPayload {
  name: string;
  email: string;
  password: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}
