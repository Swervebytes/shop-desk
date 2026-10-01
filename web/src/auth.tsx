import { createContext, useContext, useMemo, useState, type ReactNode } from "react";

const TOKEN_KEY = "shopdesk.token";
const EMAIL_KEY = "shopdesk.email";

type AuthValue = {
  token: string | null;
  email: string | null;
  setSession: (token: string, email: string) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState(() => sessionStorage.getItem(TOKEN_KEY));
  const [email, setEmail] = useState(() => sessionStorage.getItem(EMAIL_KEY));

  const value = useMemo<AuthValue>(() => ({
    token,
    email,
    setSession: (nextToken, nextEmail) => {
      sessionStorage.setItem(TOKEN_KEY, nextToken);
      sessionStorage.setItem(EMAIL_KEY, nextEmail);
      setToken(nextToken);
      setEmail(nextEmail);
    },
    logout: () => {
      sessionStorage.removeItem(TOKEN_KEY);
      sessionStorage.removeItem(EMAIL_KEY);
      setToken(null);
      setEmail(null);
    }
  }), [token, email]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("AuthProvider is missing.");
  return value;
}
