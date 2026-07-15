// 씬 간 전달용 정적 홀더 — 사망 시점의 점수/최고기록을 GameOverScene이 읽는다.
public static class GameStats
{
    public static int lastScore;
    public static int bestScore;
    public static bool isNewBest;
}
