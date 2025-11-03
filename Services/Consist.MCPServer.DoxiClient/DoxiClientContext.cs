namespace Consist.MCPServer.DoxiAPIClient
{
    public class DoxiClientContext : IEquatable<DoxiClientContext>
    {
        public string Tenant { get; set; }
        public string Username { get; set; }

        public string Password { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as DoxiClientContext);
        }

        public bool Equals(DoxiClientContext other)
        {
            if (other is null)
                return false;

            // Compare both Tenant and Username (case-sensitive or insensitive as needed)
            return string.Equals(Tenant, other.Tenant, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Username, other.Username, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            // Combine both Tenant and Username into a hash code
            return HashCode.Combine(
                Tenant?.ToLowerInvariant(),
                Username?.ToLowerInvariant()
            );
        }

        public static bool operator ==(DoxiClientContext left, DoxiClientContext right)
        {
            return EqualityComparer<DoxiClientContext>.Default.Equals(left, right);
        }

        public static bool operator !=(DoxiClientContext left, DoxiClientContext right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Tenant}:{Username}";
        }
    }
}
