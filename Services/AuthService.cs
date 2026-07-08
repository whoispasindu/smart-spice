using SmartSpice.Data;
using SmartSpice.Helpers;
using SmartSpice.Models;

namespace SmartSpice.Services;

public interface IAuthService
{
    Employee? Login(string username, string password);
    void Logout();
}


public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}

public class AuthService : IAuthService
{
    private readonly AppSession _session;
    private readonly IAuditService _audit;

    public AuthService(AppSession session, IAuditService audit)
    {
        _session = session;
        _audit = audit;
    }

    public Employee? Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new AuthenticationException("Username and password are required.");

        using var db = new SmartSpiceContext();
        var emp = db.Employees.FirstOrDefault(e => e.Username == username.Trim());

        if (emp == null || !PasswordHasher.Verify(password, emp.PasswordHash))
            throw new AuthenticationException("Invalid username or password.");

        if (!emp.IsActive)
            throw new AuthenticationException("This account has been deactivated.");

        if (!AppSession.RoleHasAccess(emp.Role))
            throw new AuthenticationException("This role does not have access to the system.");

        emp.LastLoginAt = DateTime.Now;
        db.SaveChanges();

        _session.CurrentUser = emp;
        _audit.Log("LOGIN", "Employee", $"{emp.Username} signed in.");
        return emp;
    }

    public void Logout()
    {
        if (_session.CurrentUser != null)
            _audit.Log("LOGOUT", "Employee", $"{_session.CurrentUser.Username} signed out.");
        _session.CurrentUser = null;
    }
}
