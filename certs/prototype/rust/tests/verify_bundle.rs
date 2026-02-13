use std::fs;
use std::path::Path;
use thumbprint_bundle_sample::{
    find_prototype_root, is_certificate_allowed, verify_bundle, windows_thumbprint_hex_to_x5t,
    x5t_s256_to_hex, x5t_to_windows_thumbprint_hex, DEFAULT_AUDIENCE, DEFAULT_ISSUER,
};

fn fixture_paths() -> (String, String, std::path::PathBuf, std::path::PathBuf) {
    let prototype_root = find_prototype_root(Path::new(env!("CARGO_MANIFEST_DIR"))).unwrap();
    let bundle = fs::read_to_string(prototype_root.join("bundle").join("thumbprints.bundle.jwt")).unwrap();
    let public_key = fs::read_to_string(prototype_root.join("keys").join("jwt-public.pem")).unwrap();

    let certs_root = prototype_root.parent().unwrap();
    let cert_2023 = certs_root.join("Devolutions_CodeSign_2023-2026.crt");
    let cert_2025 = certs_root.join("Devolutions_CodeSign_2025-2028.crt");

    (bundle, public_key, cert_2023, cert_2025)
}

#[test]
fn valid_bundle_verifies_and_contains_two_entries() {
    let (bundle, public_key, _, _) = fixture_paths();
    let claims = verify_bundle(bundle.trim(), &public_key, DEFAULT_ISSUER, DEFAULT_AUDIENCE).unwrap();

    assert_eq!(claims.iss, DEFAULT_ISSUER);
    assert_eq!(claims.aud, DEFAULT_AUDIENCE);
    assert_eq!(claims.thumbprints.len(), 2);
}

#[test]
fn both_repository_certificates_are_allowed() {
    let (bundle, public_key, cert_2023, cert_2025) = fixture_paths();
    let claims = verify_bundle(bundle.trim(), &public_key, DEFAULT_ISSUER, DEFAULT_AUDIENCE).unwrap();

    assert!(is_certificate_allowed(&cert_2023, &claims).unwrap());
    assert!(is_certificate_allowed(&cert_2025, &claims).unwrap());
}

#[test]
fn tampered_token_fails_validation() {
    let (bundle, public_key, _, _) = fixture_paths();
    let mut parts: Vec<String> = bundle.trim().split('.').map(ToString::to_string).collect();
    let last = parts[2].pop().unwrap();
    parts[2].push(if last == 'A' { 'B' } else { 'A' });
    let tampered = parts.join(".");

    assert!(verify_bundle(&tampered, &public_key, DEFAULT_ISSUER, DEFAULT_AUDIENCE).is_err());
}

#[test]
fn x5t_and_hex_roundtrip() {
    let (bundle, public_key, _, _) = fixture_paths();
    let claims = verify_bundle(bundle.trim(), &public_key, DEFAULT_ISSUER, DEFAULT_AUDIENCE).unwrap();

    for entry in claims.thumbprints {
        let sha1_hex = x5t_to_windows_thumbprint_hex(&entry.x5t).unwrap();
        let x5t_roundtrip = windows_thumbprint_hex_to_x5t(&sha1_hex).unwrap();
        assert_eq!(entry.x5t, x5t_roundtrip);

        let sha256_hex = x5t_s256_to_hex(&entry.x5t_s256).unwrap();
        assert_eq!(sha256_hex.len(), 64);
    }
}
