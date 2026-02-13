use anyhow::{anyhow, Context, Result};
use base64::engine::general_purpose::URL_SAFE_NO_PAD;
use base64::Engine;
use jsonwebtoken::{decode, Algorithm, DecodingKey, Validation};
use serde::{Deserialize, Serialize};
use sha1::{Digest as Sha1Digest, Sha1};
use sha2::Sha256;
use std::collections::HashSet;
use std::fs;
use std::path::Path;

pub const DEFAULT_ISSUER: &str = "https://devolutions.net/productinfo/codesign-thumbprints";
pub const DEFAULT_AUDIENCE: &str = "urn:devolutions:update-clients";

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ThumbprintBundleClaims {
    pub iss: String,
    pub aud: String,
    pub iat: u64,
    pub nbf: u64,
    pub exp: u64,
    pub ver: String,
    pub thumbprints: Vec<ThumbprintEntry>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ThumbprintEntry {
    pub x5t: String,
    #[serde(rename = "x5t#S256")]
    pub x5t_s256: String,
}

pub fn verify_bundle(jwt: &str, public_key_pem: &str, issuer: &str, audience: &str) -> Result<ThumbprintBundleClaims> {
    let mut validation = Validation::new(Algorithm::RS256);
    validation.validate_exp = true;
    validation.validate_nbf = true;
    validation.required_spec_claims = HashSet::from([
        "exp".to_string(),
        "nbf".to_string(),
        "iat".to_string(),
        "iss".to_string(),
        "aud".to_string(),
    ]);
    validation.set_issuer(&[issuer]);
    validation.set_audience(&[audience]);

    let key = DecodingKey::from_rsa_pem(public_key_pem.as_bytes())
        .context("Failed to parse RSA public key PEM")?;

    let token_data = decode::<ThumbprintBundleClaims>(jwt, &key, &validation)
        .context("JWT verification failed")?;

    Ok(token_data.claims)
}

pub fn compute_x5t_from_certificate_file(path: &Path) -> Result<String> {
    let cert_der = fs::read(path).with_context(|| format!("Failed to read certificate: {}", path.display()))?;
    let mut hasher = Sha1::new();
    hasher.update(cert_der);
    Ok(base64url_encode(&hasher.finalize()))
}

pub fn compute_x5t_s256_from_certificate_file(path: &Path) -> Result<String> {
    let cert_der = fs::read(path).with_context(|| format!("Failed to read certificate: {}", path.display()))?;
    let digest = Sha256::digest(cert_der);
    Ok(base64url_encode(&digest))
}

pub fn is_certificate_allowed(path: &Path, claims: &ThumbprintBundleClaims) -> Result<bool> {
    let x5t = compute_x5t_from_certificate_file(path)?;
    let x5t_s256 = compute_x5t_s256_from_certificate_file(path)?;

    Ok(claims
        .thumbprints
        .iter()
        .any(|entry| entry.x5t == x5t && entry.x5t_s256 == x5t_s256))
}

pub fn x5t_to_windows_thumbprint_hex(x5t: &str) -> Result<String> {
    let bytes = URL_SAFE_NO_PAD
        .decode(x5t)
        .with_context(|| format!("Invalid base64url x5t value: {x5t}"))?;
    Ok(hex::encode_upper(bytes))
}

pub fn windows_thumbprint_hex_to_x5t(hex_value: &str) -> Result<String> {
    let normalized = hex_value.replace([' ', ':'], "").to_uppercase();
    let bytes = hex::decode(&normalized)
        .with_context(|| format!("Invalid hex thumbprint value: {hex_value}"))?;
    Ok(base64url_encode(&bytes))
}

pub fn x5t_s256_to_hex(x5t_s256: &str) -> Result<String> {
    let bytes = URL_SAFE_NO_PAD
        .decode(x5t_s256)
        .with_context(|| format!("Invalid base64url x5t#S256 value: {x5t_s256}"))?;
    Ok(hex::encode_upper(bytes))
}

pub fn hex_to_x5t_s256(hex_value: &str) -> Result<String> {
    let normalized = hex_value.replace([' ', ':'], "").to_uppercase();
    let bytes = hex::decode(&normalized)
        .with_context(|| format!("Invalid hex SHA-256 thumbprint value: {hex_value}"))?;
    Ok(base64url_encode(&bytes))
}

pub fn find_prototype_root(start: &Path) -> Result<&Path> {
    let mut current = Some(start);
    while let Some(path) = current {
        let bundle = path.join("bundle").join("thumbprints.bundle.jwt");
        let key = path.join("keys").join("jwt-public.pem");
        if bundle.exists() && key.exists() {
            return Ok(path);
        }
        current = path.parent();
    }

    Err(anyhow!("Could not locate certs/prototype root"))
}

fn base64url_encode(bytes: &[u8]) -> String {
    URL_SAFE_NO_PAD.encode(bytes)
}
