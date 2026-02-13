use std::fs;
use std::path::Path;
use thumbprint_bundle_sample::{
    compute_sha1_thumbprint_hex_from_certificate_file, find_prototype_root, is_certificate_allowed,
    verify_bundle, DEFAULT_AUDIENCE, DEFAULT_ISSUER,
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
    let (bundle, public_key, cert_2023, cert_2025) = fixture_paths();
    let claims = verify_bundle(bundle.trim(), &public_key, DEFAULT_ISSUER, DEFAULT_AUDIENCE).unwrap();

    assert_eq!(claims.iss, DEFAULT_ISSUER);
    assert_eq!(claims.aud, DEFAULT_AUDIENCE);
    assert_eq!(claims.thumbprints.len(), 2);
    assert!(claims.thumbprints.iter().all(|entry| entry.len() == 40 && entry.chars().all(|c| c.is_ascii_hexdigit() && !c.is_ascii_lowercase())));

    let newest_thumbprint = compute_sha1_thumbprint_hex_from_certificate_file(&cert_2025).unwrap();
    let older_thumbprint = compute_sha1_thumbprint_hex_from_certificate_file(&cert_2023).unwrap();
    assert_eq!(claims.thumbprints[0], newest_thumbprint);
    assert_eq!(claims.thumbprints[1], older_thumbprint);
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
fn computed_sha1_thumbprints_are_present_in_bundle() {
    let (bundle, public_key, cert_2023, cert_2025) = fixture_paths();
    let claims = verify_bundle(bundle.trim(), &public_key, DEFAULT_ISSUER, DEFAULT_AUDIENCE).unwrap();

    let cert2025_thumbprint = compute_sha1_thumbprint_hex_from_certificate_file(&cert_2025).unwrap();
    let cert2023_thumbprint = compute_sha1_thumbprint_hex_from_certificate_file(&cert_2023).unwrap();

    assert!(claims.thumbprints.contains(&cert2025_thumbprint));
    assert!(claims.thumbprints.contains(&cert2023_thumbprint));
}
