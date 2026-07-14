// GetGemPackages — GEM 충전 상품표(서버 권위). 클라는 이 목록으로 상품 UI를 그리고,
// 결제 확정(CapturePaypalOrder)에서 sku→가격/지급GEM을 다시 서버에서 검증한다(가격 위조 방지).
// 가격은 조정 가능. priceUsd는 PayPal 주문 금액(USD).

const PACKAGES = [
  { sku: "gem_1000",  gem: 1000,  priceUsd: "0.99",  label: "1,000 GEM" },
  { sku: "gem_5500",  gem: 5500,  priceUsd: "4.99",  label: "5,500 GEM",  bonus: "+10%" },
  { sku: "gem_12000", gem: 12000, priceUsd: "9.99",  label: "12,000 GEM", bonus: "+20%" },
  { sku: "gem_25000", gem: 25000, priceUsd: "19.99", label: "25,000 GEM", bonus: "+25%" }
];

module.exports = async ({ context, logger }) => {
  return { packages: PACKAGES };
};
