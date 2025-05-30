namespace GjammT.Models.Auth;

public class Saml2Configuration
{
    public Guid Id { get; set; }
    
    // Core SAML settings
    public string Issuer { get; set; }
    public string IdPMetadataUrl { get; set; }
    public string IdPEntityId { get; set; }
    public string SingleSignOnUrl { get; set; }
    public string SingleLogoutUrl { get; set; }
    
    // Certificate data (store securely!)
    public string CertificateBase64 { get; set; }
    public string PrivateKeyBase64 { get; set; }
    
    // Additional settings
    public string SignatureAlgorithm { get; set; } = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    public bool ForceAuth { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}