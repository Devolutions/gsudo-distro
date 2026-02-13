use anyhow::Result;
use std::fs;
use std::path::Path;
use thumbprint_bundle_sample::{
    find_prototype_root, is_certificate_allowed, verify_bundle, DEFAULT_AUDIENCE, DEFAULT_ISSUER,
};

fn main() -> Result<()> {
    let prototype_root = find_prototype_root(Path::new(env!("CARGO_MANIFEST_DIR")))?;

    let bundle_path = prototype_root.join("bundle").join("thumbprints.bundle.jwt");
    let public_key_path = prototype_root.join("keys").join("jwt-public.pem");
    let certs_root = prototype_root.parent().expect("prototype folder must be under certs");

    let cert_paths = [
        certs_root.join("Devolutions_CodeSign_2023-2026.crt"),
        certs_root.join("Devolutions_CodeSign_2025-2028.crt"),
    ];

    let jwt = fs::read_to_string(bundle_path)?;
    let public_key = fs::read_to_string(public_key_path)?;

    let claims = verify_bundle(jwt.trim(), &public_key, DEFAULT_ISSUER, DEFAULT_AUDIENCE)?;
    println!("Bundle verified. version={}, entries={}", claims.ver, claims.thumbprints.len());

    for cert_path in cert_paths {
        let allowed = is_certificate_allowed(&cert_path, &claims)?;
        println!(
            "  {}: {}",
            cert_path.file_name().unwrap_or_default().to_string_lossy(),
            if allowed { "ALLOWED" } else { "BLOCKED" }
        );
    }

    Ok(())
}
