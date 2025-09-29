import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 5,
  duration: '30s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export default function () {
  const payload = JSON.stringify({ query: 'charity guidance', page: 1, pageSize: 10 });
  const headers = { 'Content-Type': 'application/json' };
  const res = http.post(`${BASE_URL}/tools/search_guidance`, payload, { headers });
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  sleep(0.5);
}
