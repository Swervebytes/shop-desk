import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../auth";

export function Shell() {
  const { email, logout } = useAuth();
  const navigate = useNavigate();

  return (
    <div className="app">
      <header className="topbar">
        <NavLink to="/jobs" className="brand" data-testid="brand">Shop Desk</NavLink>
        <nav aria-label="Primary">
          <NavLink to="/jobs" className={({ isActive }) => isActive ? "nav on" : "nav"} data-testid="nav-jobs">Jobs</NavLink>
          <NavLink to="/clients" className={({ isActive }) => isActive ? "nav on" : "nav"} data-testid="nav-clients">Clients</NavLink>
        </nav>
        <div className="session">
          <span className="who" data-testid="signed-in-email">{email}</span>
          <button
            type="button"
            className="ghost"
            data-testid="logout"
            onClick={() => {
              logout();
              navigate("/login");
            }}
          >
            Log out
          </button>
        </div>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}
