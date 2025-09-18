function toCamel(key) {
  // if key is ALL CAPS (like HRA, PF), make it fully lowercased
  if (/^[A-Z0-9_]+$/.test(key)) {
    return key.toLowerCase();
  }
  // otherwise, only lowercase the first char
  return key.charAt(0).toLowerCase() + key.slice(1);
}

export const normalizeKeys = (data) => {
  if (Array.isArray(data)) {
    return data.map(normalizeKeys);
  } else if (data !== null && typeof data === "object") {
    return Object.keys(data).reduce((acc, key) => {
      const camelKey = toCamel(key);
      acc[camelKey] = normalizeKeys(data[key]);
      return acc;
    }, {});
  }
  return data;
};