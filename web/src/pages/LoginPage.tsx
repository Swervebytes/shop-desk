import { useState, type FormEvent } from "react";
import { Navigate } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";

export function LoginPage() {
  const { token, setSession } = useAuth();
  const [email, setEmail] = useState("demo@shopdesk.dev");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  if (token) return <Navigate to="/jobs" replace />;

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError("");
    setPending(true);
    try {
      const result = await api<{ token: string; email: string }>("/api/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password })
      });
      setSession(result.token, result.email);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Sign in failed.");
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="login-screen">
      <form className="login-card" onSubmit={onSubmit}>
        <p className="eyebrow">Software shop</p>
        <h1>Shop Desk</h1>
        <p className="lede">Sign in to the job board.</p>
        <label>
          Email
          <input
            data-testid="login-email"
            type="email"
            autoComplete="username"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </label>
        <label>
          Password
          <input
            data-testid="login-password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </label>
        {error ? <p className="form-error" role="alert" data-testid="login-error">{error}</p> : null}
        <button type="submit" data-testid="login-submit" disabled={pending}>
          {pending ? "Signing in…" : "Sign in"}
        </button>
        <dl className="demo-account">
          <div>
            <dt>Demo email</dt>
            <dd data-testid="demo-email">demo@shopdesk.dev</dd>
          </div>
          <div>
            <dt>Demo password</dt>
            <dd data-testid="demo-password">demo-shop-desk</dd>
          </div>
        </dl>
      </form>
    </div>
  );
}
