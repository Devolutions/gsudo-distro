use anyhow::{anyhow, Context, Result};
use jsonwebtoken::{decode, Algorithm, DecodingKey, Validation};
use serde::{Deserialize, Serialize};
use sha1::{Digest as Sha1Digest, Sha1};
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
    pub thumbprints: Vec<String>,
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

pub fn compute_sha1_thumbprint_hex_from_certificate_file(path: &Path) -> Result<String> {
    let cert_der = fs::read(path).with_context(|| format!("Failed to read certificate: {}", path.display()))?;
    let mut hasher = Sha1::new();
    hasher.update(cert_der);
    Ok(hex::encode_upper(hasher.finalize()))
}

pub fn is_certificate_allowed(path: &Path, claims: &ThumbprintBundleClaims) -> Result<bool> {
    let thumbprint_hex = compute_sha1_thumbprint_hex_from_certificate_file(path)?;
    Ok(claims.thumbprints.iter().any(|entry| entry == &thumbprint_hex))
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
